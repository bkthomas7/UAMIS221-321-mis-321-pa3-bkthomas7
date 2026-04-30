using Pa3Api.Models;
using Pa3Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy.AllowAnyHeader()
            .AllowAnyMethod()
            .AllowAnyOrigin();
    });
});

builder.Services.AddHttpClient<LlmService>();
builder.Services.AddSingleton<DatabaseService>();
builder.Services.AddSingleton<FunctionService>();

var app = builder.Build();

app.UseCors("frontend");
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/health", () => Results.Ok(new { ok = true }));

app.MapPost("/api/chat", async (
    ChatRequest request,
    LlmService llmService,
    DatabaseService dbService,
    FunctionService functionService) =>
{
    if (string.IsNullOrWhiteSpace(request.Message))
    {
        return Results.BadRequest(new { error = "Message is required." });
    }

    try
    {
        var ragChunks = await dbService.GetRelevantContextAsync(request.Message);
        var reply = await llmService.AskWithRagAndFunctionCallingAsync(
            request.Message,
            ragChunks,
            functionService);

        await dbService.SaveConversationAsync(request.Message, reply);

        return Results.Ok(new ChatResponse { Reply = reply });
    }
    catch (Exception ex)
    {
        return Results.Ok(new ChatResponse
        {
            Reply = $"I could not reach the LLM right now. Technical message: {ex.Message}"
        });
    }
});

app.Run();
