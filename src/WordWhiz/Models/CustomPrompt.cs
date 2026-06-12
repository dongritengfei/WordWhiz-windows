namespace WordWhiz.Models;

/// <summary>
/// Represents a custom prompt template for text optimization.
/// </summary>
public class CustomPrompt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string PromptTemplate { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public int SortOrder { get; set; }

    /// <summary>
    /// Gets a preview of the prompt template (first 2 lines).
    /// </summary>
    public string Preview
    {
        get
        {
            var lines = PromptTemplate.Split('\n');
            return string.Join(" ", lines.Take(2)).Trim();
        }
    }
}
