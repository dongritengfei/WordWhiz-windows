using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using WordWhiz.Helpers;

namespace WordWhiz.Services.LLM;

/// <summary>
/// Anthropic Messages API provider with SSE streaming.
/// Uses a different request/response format than OpenAI.
/// </summary>
public class AnthropicProvider : ILLMProvider
{
    private readonly HttpClient _httpClient;
    private readonly Uri _baseUrl;
    private readonly string _modelName;
    private readonly string _apiKey;

    public string ProviderName => "Anthropic";

    public AnthropicProvider(string apiKey, string modelName = "claude-sonnet-4-20250514",
        string? baseUrl = null)
    {
        _apiKey = apiKey;
        _modelName = modelName;
        _baseUrl = new Uri((baseUrl ?? "https://api.anthropic.com").TrimEnd('/') + "/");

        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(Constants.RequestTimeoutSeconds)
        };
    }

    public async IAsyncEnumerable<string> StreamOptimizeAsync(
        string systemPrompt,
        string userPrompt,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var url = new Uri(_baseUrl, "v1/messages");
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("x-api-key", _apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");

        var body = new
        {
            model = _modelName,
            max_tokens = 2000,
            system = systemPrompt,
            messages = new[]
            {
                new { role = "user", content = userPrompt }
            },
            stream = true
        };

        request.Content = new StringContent(
            JsonSerializer.Serialize(body),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.SendAsync(request,
            HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new LLMException($"HTTP {(int)response.StatusCode}: {errorBody[..Math.Min(200, errorBody.Length)]}",
                (int)response.StatusCode);
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = await reader.ReadLineAsync(cancellationToken);
            if (line == null) break;

            var trimmed = line.Trim();

            // Check for message_stop event
            var eventType = SSEParser.ParseEventType(trimmed);
            if (eventType == "message_stop")
                break;

            // Skip empty lines and comments
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(':'))
                continue;

            // Parse data line
            var dataContent = SSEParser.ParseDataLine(trimmed);
            if (dataContent == null) continue;

            // Extract content from content_block_delta events
            var content = SSEParser.ExtractAnthropicContent(dataContent);
            if (content != null)
            {
                yield return content;
            }
        }
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var url = new Uri(_baseUrl, "v1/messages");
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("x-api-key", _apiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");

            var body = new
            {
                model = _modelName,
                max_tokens = 5,
                messages = new[] { new { role = "user", content = "Hi" } }
            };

            request.Content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json");

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            var response = await _httpClient.SendAsync(request, cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
