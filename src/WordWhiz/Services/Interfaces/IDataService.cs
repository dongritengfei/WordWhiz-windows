using WordWhiz.Models;

namespace WordWhiz.Services.Interfaces;

/// <summary>
/// Data persistence service interface.
/// </summary>
public interface IDataService
{
    Task InitializeAsync();

    // Prompt CRUD
    Task<List<CustomPrompt>> GetPromptsAsync();
    Task<CustomPrompt?> GetPromptByIdAsync(Guid id);
    Task InsertPromptAsync(CustomPrompt prompt);
    Task UpdatePromptAsync(CustomPrompt prompt);
    Task DeletePromptAsync(Guid id);
    Task UpdatePromptSortOrderAsync(List<(Guid id, int order)> orders);

    // Optimization records
    Task<List<OptimizationRecord>> GetRecordsAsync(int limit = 50, int offset = 0);
    Task<List<OptimizationRecord>> SearchRecordsAsync(string query, int limit = 50);
    Task InsertRecordAsync(OptimizationRecord record);
    Task DeleteRecordAsync(Guid id);
    Task ClearAllRecordsAsync();
    Task<int> GetRecordCountAsync();

    // Settings (key-value store)
    Task<T> GetSettingAsync<T>(string key, T defaultValue);
    Task SetSettingAsync<T>(string key, T value);
}
