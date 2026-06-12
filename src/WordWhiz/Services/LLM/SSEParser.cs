using System.Text.Json;

namespace WordWhiz.Services.LLM;

/// <summary>
/// Generic SSE (Server-Sent Events) line parser.
/// </summary>
public static class SSEParser
{
    /// <summary>
    /// Parse a single SSE data line and extract the JSON content.
    /// Returns null for comments, empty lines, and non-data lines.
    /// </summary>
    public static string? ParseDataLine(string line)
    {
        var trimmed = line.Trim();

        // Skip empty lines and comments
        if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(':'))
            return null;

        // Extract data field
        if (trimmed.StartsWith("data:"))
        {
            var dataContent = trimmed[5..].Trim();

            // Check for stream termination
            if (dataContent == "[DONE]")
                return null;

            return dataContent;
        }

        return null;
    }

    /// <summary>
    /// Check if a line signals stream completion.
    /// </summary>
    public static bool IsStreamEnd(string line)
    {
        var trimmed = line.Trim();
        return trimmed is "data: [DONE]" or "data:[DONE]";
    }

    /// <summary>
    /// Parse event type from an SSE line (e.g., "event: content_block_delta").
    /// </summary>
    public static string? ParseEventType(string line)
    {
        var trimmed = line.Trim();
        if (trimmed.StartsWith("event:"))
        {
            return trimmed[6..].Trim();
        }
        return null;
    }

    /// <summary>
    /// Extract content delta from OpenAI-format JSON response.
    /// Format: {"choices":[{"delta":{"content":"text"}}]}
    /// </summary>
    public static string? ExtractOpenAIContent(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("choices", out var choices) &&
                choices.GetArrayLength() > 0)
            {
                var firstChoice = choices[0];
                if (firstChoice.TryGetProperty("delta", out var delta) &&
                    delta.TryGetProperty("content", out var content))
                {
                    return content.GetString();
                }
            }
        }
        catch (JsonException)
        {
            // Skip malformed JSON
        }
        return null;
    }

    /// <summary>
    /// Extract text delta from Anthropic-format JSON response.
    /// Format: {"type":"content_block_delta","delta":{"type":"text_delta","text":"Hello"}}
    /// </summary>
    public static string? ExtractAnthropicContent(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("type", out var type) &&
                type.GetString() == "content_block_delta" &&
                root.TryGetProperty("delta", out var delta) &&
                delta.TryGetProperty("text", out var text))
            {
                return text.GetString();
            }
        }
        catch (JsonException)
        {
            // Skip malformed JSON
        }
        return null;
    }
}
