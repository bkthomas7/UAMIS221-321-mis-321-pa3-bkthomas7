using Dapper;
using MySqlConnector;

namespace Pa3Api.Services;

public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Missing DB connection string.");
    }

    public async Task<List<string>> GetRelevantContextAsync(string userMessage)
    {
        var words = userMessage
            .ToLower()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(w => w.Length > 2)
            .Take(4)
            .ToList();

        if (words.Count == 0)
        {
            return new List<string>();
        }

        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        var clauses = new List<string>();
        var parameters = new DynamicParameters();
        for (int i = 0; i < words.Count; i++)
        {
            clauses.Add($"content LIKE @w{i}");
            parameters.Add($"w{i}", $"%{words[i]}%");
        }

        var sql = $@"
            SELECT content
            FROM knowledge_base
            WHERE {string.Join(" OR ", clauses)}
            LIMIT 3;";

        var rows = await connection.QueryAsync<string>(sql, parameters);
        return rows.ToList();
    }

    public async Task SaveConversationAsync(string userMessage, string assistantReply)
    {
        const string sql = @"
            INSERT INTO conversation_history(user_message, assistant_reply)
            VALUES(@userMessage, @assistantReply);";

        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        await connection.ExecuteAsync(sql, new { userMessage, assistantReply });
    }

    public async Task<int> CountSavedMessagesAsync()
    {
        const string sql = "SELECT COUNT(*) FROM conversation_history;";
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        return await connection.ExecuteScalarAsync<int>(sql);
    }
}
