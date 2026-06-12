namespace WordWhiz.Services.LLM;

/// <summary>
/// Interface for LLM provider implementations.
/// </summary>
public interface ILLMProvider
{
    /// <summary>
    /// Stream optimization results from the LLM.
    /// </summary>
    IAsyncEnumerable<string> StreamOptimizeAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Test the API connection with a minimal request.
    /// </summary>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Provider name for display.
    /// </summary>
    string ProviderName { get; }
}

/// <summary>
/// LLM error types.
/// </summary>
public class LLMException : Exception
{
    public int? StatusCode { get; }

    public LLMException(string message, int? statusCode = null)
        : base(message)
    {
        StatusCode = statusCode;
    }
}
