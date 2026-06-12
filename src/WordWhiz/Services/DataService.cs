using System.Text.Json;
using Dapper;
using Microsoft.Data.Sqlite;
using WordWhiz.Helpers;
using WordWhiz.Models;
using WordWhiz.Services.Interfaces;

namespace WordWhiz.Services;

/// <summary>
/// SQLite-based data persistence service.
/// Database location: %LOCALAPPDATA%\WordWhiz\data\wordwhiz.db
/// </summary>
public class DataService : IDataService, IDisposable
{
    private SqliteConnection? _connection;
    private readonly string _dbPath;

    public DataService()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dataDir = Path.Combine(localAppData, Constants.AppFolderName, Constants.DataFolderName);
        Directory.CreateDirectory(dataDir);
        _dbPath = Path.Combine(dataDir, Constants.DatabaseFileName);
    }

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection($"Data Source={_dbPath}");
        await _connection.OpenAsync();

        await CreateTablesAsync();
        await SeedDefaultPromptsAsync();
    }

    private async Task CreateTablesAsync()
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS custom_prompts (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                prompt_template TEXT NOT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL,
                sort_order INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS optimization_records (
                id TEXT PRIMARY KEY,
                source_text TEXT NOT NULL,
                result_text TEXT NOT NULL,
                prompt_name TEXT,
                created_at TEXT NOT NULL,
                character_count INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS app_settings (
                key TEXT PRIMARY KEY,
                value TEXT NOT NULL
            );
            """;

        await _connection!.ExecuteAsync(sql);
    }

    private async Task SeedDefaultPromptsAsync()
    {
        var count = await _connection!.QuerySingleAsync<int>("SELECT COUNT(*) FROM custom_prompts");
        if (count > 0) return;

        var defaults = DefaultPrompts.GetDefaults();
        foreach (var prompt in defaults)
        {
            await InsertPromptAsync(prompt);
        }
    }

    // ── Prompts ──────────────────────────────────────────────

    public async Task<List<CustomPrompt>> GetPromptsAsync()
    {
        const string sql = "SELECT * FROM custom_prompts ORDER BY sort_order ASC";
        var rows = await _connection!.QueryAsync<PromptRow>(sql);
        return rows.Select(MapToPrompt).ToList();
    }

    public async Task<CustomPrompt?> GetPromptByIdAsync(Guid id)
    {
        const string sql = "SELECT * FROM custom_prompts WHERE id = @Id";
        var row = await _connection!.QueryFirstOrDefaultAsync<PromptRow>(sql, new { Id = id.ToString() });
        return row == null ? null : MapToPrompt(row);
    }

    public async Task InsertPromptAsync(CustomPrompt prompt)
    {
        const string sql = """
            INSERT INTO custom_prompts (id, name, prompt_template, created_at, updated_at, sort_order)
            VALUES (@Id, @Name, @PromptTemplate, @CreatedAt, @UpdatedAt, @SortOrder)
            """;
        await _connection!.ExecuteAsync(sql, new
        {
            Id = prompt.Id.ToString(),
            Name = prompt.Name,
            PromptTemplate = prompt.PromptTemplate,
            CreatedAt = prompt.CreatedAt.ToString("o"),
            UpdatedAt = prompt.UpdatedAt.ToString("o"),
            SortOrder = prompt.SortOrder
        });
    }

    public async Task UpdatePromptAsync(CustomPrompt prompt)
    {
        prompt.UpdatedAt = DateTime.Now;
        const string sql = """
            UPDATE custom_prompts
            SET name = @Name, prompt_template = @PromptTemplate, updated_at = @UpdatedAt, sort_order = @SortOrder
            WHERE id = @Id
            """;
        await _connection!.ExecuteAsync(sql, new
        {
            Id = prompt.Id.ToString(),
            Name = prompt.Name,
            PromptTemplate = prompt.PromptTemplate,
            UpdatedAt = prompt.UpdatedAt.ToString("o"),
            SortOrder = prompt.SortOrder
        });
    }

    public async Task DeletePromptAsync(Guid id)
    {
        const string sql = "DELETE FROM custom_prompts WHERE id = @Id";
        await _connection!.ExecuteAsync(sql, new { Id = id.ToString() });
    }

    public async Task UpdatePromptSortOrderAsync(List<(Guid id, int order)> orders)
    {
        using var transaction = _connection!.BeginTransaction();
        try
        {
            foreach (var (id, order) in orders)
            {
                await _connection.ExecuteAsync(
                    "UPDATE custom_prompts SET sort_order = @Order WHERE id = @Id",
                    new { Id = id.ToString(), Order = order },
                    transaction);
            }
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    // ── Records ──────────────────────────────────────────────

    public async Task<List<OptimizationRecord>> GetRecordsAsync(int limit = 50, int offset = 0)
    {
        const string sql = "SELECT * FROM optimization_records ORDER BY created_at DESC LIMIT @Limit OFFSET @Offset";
        var rows = await _connection!.QueryAsync<RecordRow>(sql, new { Limit = limit, Offset = offset });
        return rows.Select(MapToRecord).ToList();
    }

    public async Task<List<OptimizationRecord>> SearchRecordsAsync(string query, int limit = 50)
    {
        const string sql = """
            SELECT * FROM optimization_records
            WHERE source_text LIKE @Query OR result_text LIKE @Query OR prompt_name LIKE @Query
            ORDER BY created_at DESC LIMIT @Limit
            """;
        var rows = await _connection!.QueryAsync<RecordRow>(sql,
            new { Query = $"%{query}%", Limit = limit });
        return rows.Select(MapToRecord).ToList();
    }

    public async Task InsertRecordAsync(OptimizationRecord record)
    {
        const string sql = """
            INSERT INTO optimization_records (id, source_text, result_text, prompt_name, created_at, character_count)
            VALUES (@Id, @SourceText, @ResultText, @PromptName, @CreatedAt, @CharacterCount)
            """;
        await _connection!.ExecuteAsync(sql, new
        {
            Id = record.Id.ToString(),
            SourceText = record.SourceText,
            ResultText = record.ResultText,
            PromptName = record.PromptName,
            CreatedAt = record.CreatedAt.ToString("o"),
            CharacterCount = record.CharacterCount
        });
    }

    public async Task DeleteRecordAsync(Guid id)
    {
        const string sql = "DELETE FROM optimization_records WHERE id = @Id";
        await _connection!.ExecuteAsync(sql, new { Id = id.ToString() });
    }

    public async Task ClearAllRecordsAsync()
    {
        const string sql = "DELETE FROM optimization_records";
        await _connection!.ExecuteAsync(sql);
    }

    public async Task<int> GetRecordCountAsync()
    {
        const string sql = "SELECT COUNT(*) FROM optimization_records";
        return await _connection!.QuerySingleAsync<int>(sql);
    }

    // ── Settings ─────────────────────────────────────────────

    public async Task<T> GetSettingAsync<T>(string key, T defaultValue)
    {
        const string sql = "SELECT value FROM app_settings WHERE key = @Key";
        var value = await _connection!.QueryFirstOrDefaultAsync<string>(sql, new { Key = key });
        if (value == null) return defaultValue;

        try
        {
            return JsonSerializer.Deserialize<T>(value) ?? defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    public async Task SetSettingAsync<T>(string key, T value)
    {
        var json = JsonSerializer.Serialize(value);
        const string sql = """
            INSERT INTO app_settings (key, value) VALUES (@Key, @Value)
            ON CONFLICT(key) DO UPDATE SET value = @Value
            """;
        await _connection!.ExecuteAsync(sql, new { Key = key, Value = json });
    }

    // ── Mapping ──────────────────────────────────────────────

    private static CustomPrompt MapToPrompt(PromptRow row) => new()
    {
        Id = Guid.Parse(row.id),
        Name = row.name,
        PromptTemplate = row.prompt_template,
        CreatedAt = DateTime.Parse(row.created_at),
        UpdatedAt = DateTime.Parse(row.updated_at),
        SortOrder = row.sort_order
    };

    private static OptimizationRecord MapToRecord(RecordRow row) => new()
    {
        Id = Guid.Parse(row.id),
        SourceText = row.source_text,
        ResultText = row.result_text,
        PromptName = row.prompt_name,
        CreatedAt = DateTime.Parse(row.created_at),
        CharacterCount = row.character_count
    };

    // Dapper row types
    // ReSharper disable ClassNeverInstantiated.Local
    private class PromptRow
    {
        public string id { get; set; } = "";
        public string name { get; set; } = "";
        public string prompt_template { get; set; } = "";
        public string created_at { get; set; } = "";
        public string updated_at { get; set; } = "";
        public int sort_order { get; set; }
    }

    private class RecordRow
    {
        public string id { get; set; } = "";
        public string source_text { get; set; } = "";
        public string result_text { get; set; } = "";
        public string? prompt_name { get; set; }
        public string created_at { get; set; } = "";
        public int character_count { get; set; }
    }
    // ReSharper restore ClassNeverInstantiated.Local

    public void Dispose()
    {
        _connection?.Dispose();
    }
}
