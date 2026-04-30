using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Pa3Api.Services;

public class LlmService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiUrl;
    private readonly string _apiKey;
    private readonly string _model;

    public LlmService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiUrl = configuration["Llm:ApiUrl"] ?? "";
        _apiKey = configuration["Llm:ApiKey"] ?? "";
        _model = configuration["Llm:Model"] ?? "gpt-4o-mini";
    }

    public async Task<string> AskWithRagAndFunctionCallingAsync(
        string userMessage,
        List<string> ragChunks,
        FunctionService functionService)
    {
        try
        {
            return await AskOpenAiWithToolsAsync(userMessage, ragChunks, functionService);
        }
        catch
        {
            return await BuildLocalFallbackReplyAsync(userMessage, ragChunks, functionService);
        }
    }

    private async Task<string> AskOpenAiWithToolsAsync(
        string userMessage,
        List<string> ragChunks,
        FunctionService functionService)
    {
        var ragText = ragChunks.Count == 0
            ? "No extra context was found."
            : string.Join("\n- ", ragChunks.Prepend("Relevant context:"));

        var messages = new JsonArray
        {
            new JsonObject
            {
                ["role"] = "system",
                ["content"] = "You are a helpful MIS student assistant. Use provided context when relevant."
            },
            new JsonObject
            {
                ["role"] = "system",
                ["content"] = ragText
            },
            new JsonObject
            {
                ["role"] = "user",
                ["content"] = userMessage
            }
        };

        var tools = new JsonArray
        {
            new JsonObject
            {
                ["type"] = "function",
                ["function"] = new JsonObject
                {
                    ["name"] = "get_current_time",
                    ["description"] = "Get current local date and time."
                }
            },
            new JsonObject
            {
                ["type"] = "function",
                ["function"] = new JsonObject
                {
                    ["name"] = "count_saved_messages",
                    ["description"] = "Count saved messages in database."
                }
            },
            new JsonObject
            {
                ["type"] = "function",
                ["function"] = new JsonObject
                {
                    ["name"] = "simple_math",
                    ["description"] = "Add two numbers together.",
                    ["parameters"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject
                        {
                            ["a"] = new JsonObject { ["type"] = "string" },
                            ["b"] = new JsonObject { ["type"] = "string" }
                        },
                        ["required"] = new JsonArray { "a", "b" }
                    }
                }
            }
        };

        var firstResponse = await SendCompletionRequestAsync(messages, tools);
        var firstChoice = firstResponse?["choices"]?[0]?["message"];
        if (firstChoice == null)
        {
            return "No response from LLM.";
        }

        var toolCall = firstChoice["tool_calls"]?[0];
        if (toolCall == null)
        {
            return firstChoice["content"]?.ToString() ?? "No text response.";
        }

        var functionName = toolCall["function"]?["name"]?.ToString() ?? "";
        var argumentsText = toolCall["function"]?["arguments"]?.ToString() ?? "{}";
        var parsedArgs = ParseArguments(argumentsText);
        var functionResult = await functionService.ExecuteAsync(functionName, parsedArgs);

        messages.Add(new JsonObject
        {
            ["role"] = "assistant",
            ["content"] = firstChoice["content"]?.ToString() ?? "",
            ["tool_calls"] = firstChoice["tool_calls"]?.DeepClone()
        });

        messages.Add(new JsonObject
        {
            ["role"] = "tool",
            ["tool_call_id"] = toolCall["id"]?.ToString(),
            ["content"] = functionResult
        });

        var secondResponse = await SendCompletionRequestAsync(messages, tools);
        var finalText = secondResponse?["choices"]?[0]?["message"]?["content"]?.ToString();
        return string.IsNullOrWhiteSpace(finalText) ? functionResult : finalText;
    }

    private async Task<string> BuildLocalFallbackReplyAsync(
        string userMessage,
        List<string> ragChunks,
        FunctionService functionService)
    {
        var lower = userMessage.ToLower();

        if (lower.Contains("time") || lower.Contains("date"))
        {
            var time = await functionService.ExecuteAsync("get_current_time", new Dictionary<string, string>());
            return $"[Local fallback mode] {time}";
        }

        if (lower.Contains("how many") && lower.Contains("message"))
        {
            var count = await functionService.ExecuteAsync("count_saved_messages", new Dictionary<string, string>());
            return $"[Local fallback mode] {count}";
        }

        var mathNumbers = ExtractFirstTwoNumbers(lower);
        if (mathNumbers.HasValue && (lower.Contains("add") || lower.Contains("plus") || lower.Contains("+")))
        {
            var args = new Dictionary<string, string>
            {
                ["a"] = mathNumbers.Value.a.ToString(),
                ["b"] = mathNumbers.Value.b.ToString()
            };
            var mathResult = await functionService.ExecuteAsync("simple_math", args);
            return $"[Local fallback mode] {mathResult}";
        }

        if (ragChunks.Count > 0)
        {
            return "[Local fallback mode] Based on the project knowledge base: "
                + ragChunks[0];
        }

        return "[Local fallback mode] I am running without the external LLM API right now, "
            + "but your app is working. Try asking for time, a simple add question, or RAG topics like 'what is rag'.";
    }

    private async Task<JsonNode?> SendCompletionRequestAsync(JsonArray messages, JsonArray tools)
    {
        var payload = new JsonObject
        {
            ["model"] = _model,
            ["messages"] = messages,
            ["tools"] = tools,
            ["tool_choice"] = "auto",
            ["temperature"] = 0.3
        };

        var request = new HttpRequestMessage(HttpMethod.Post, _apiUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        request.Content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);
        var jsonText = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"LLM call failed: {response.StatusCode} {jsonText}");
        }

        return JsonNode.Parse(jsonText);
    }

    private static Dictionary<string, string> ParseArguments(string argumentsText)
    {
        var output = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var obj = JsonNode.Parse(argumentsText)?.AsObject();
            if (obj == null)
            {
                return output;
            }

            foreach (var pair in obj)
            {
                output[pair.Key] = pair.Value?.ToString() ?? "";
            }
        }
        catch
        {
            return output;
        }

        return output;
    }

    private static (decimal a, decimal b)? ExtractFirstTwoNumbers(string text)
    {
        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var found = new List<decimal>();
        foreach (var part in parts)
        {
            var cleaned = new string(part.Where(c => char.IsDigit(c) || c == '.' || c == '-').ToArray());
            if (decimal.TryParse(cleaned, out var value))
            {
                found.Add(value);
                if (found.Count == 2)
                {
                    return (found[0], found[1]);
                }
            }
        }

        return null;
    }
}
