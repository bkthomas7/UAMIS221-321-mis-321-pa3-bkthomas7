namespace Pa3Api.Services;

public class FunctionService
{
    private readonly DatabaseService _databaseService;

    public FunctionService(DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task<string> ExecuteAsync(string functionName, Dictionary<string, string> args)
    {
        if (functionName == "get_current_time")
        {
            return DateTime.Now.ToString("F");
        }

        if (functionName == "count_saved_messages")
        {
            var count = await _databaseService.CountSavedMessagesAsync();
            return $"There are {count} saved messages in conversation_history.";
        }

        if (functionName == "simple_math")
        {
            if (!args.TryGetValue("a", out var aText) || !args.TryGetValue("b", out var bText))
            {
                return "Missing numbers for simple_math.";
            }

            if (!decimal.TryParse(aText, out var a) || !decimal.TryParse(bText, out var b))
            {
                return "Could not parse numbers for simple_math.";
            }

            return $"Result: {a + b}";
        }

        return $"Unknown function: {functionName}";
    }
}
