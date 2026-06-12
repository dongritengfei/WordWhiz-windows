namespace WordWhiz.Models;

/// <summary>
/// Represents a single optimization history record.
/// </summary>
public class OptimizationRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string SourceText { get; set; } = string.Empty;
    public string ResultText { get; set; } = string.Empty;
    public string? PromptName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int CharacterCount { get; set; }
}
