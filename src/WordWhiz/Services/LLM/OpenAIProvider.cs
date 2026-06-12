using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using WordWhiz.Helpers;

namespace WordWhiz.Services.LLM;

/// <summary>
/// OpenAI-compatible chat completions provider with SSE streaming.
/// Also serves as base for DeepSeek, Qwen, and custom OpenAI-compatible providers.
/// </summary>
public class OpenAIProvider : ILLMProvider
{
    protected readonly HttpClient HttpClient;
    protected readonly Uri BaseUrl;
    protected readonly string ModelName;
    protected readonly string ApiKey;

    public virtual string ProviderName => "OpenAI";

    public OpenAIProvider(string apiKey, string baseUrl, string modelName)
    {
        ApiKey = apiKey;
        BaseUrl = new Uri(baseUrl.TrimEnd('/') + "/");
        ModelName = modelName;

        HttpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(Constants.RequestTimeoutSeconds)
        };
    }

    public async IAsyncEnumerable<string> StreamOptimizeAsync(
        string systemPrompt,
        string userPrompt,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var url = new Uri(BaseUrl, "chat/completions");
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        var body = new
        {
            model = ModelName,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            stream = true,
            temperature = 0.7,
            max_tokens = 2000
        };

        request.Content = new StringContent(
            JsonSerializer.Serialize(body),
            Encoding.UTF8,
            "application/json");

        var response = await HttpClient.SendAsync(request,
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

            if (SSEParser.IsStreamEnd(line))
                break;

            var dataContent = SSEParser.ParseDataLine(line);
            if (dataContent == null) continue;

            var content = SSEParser.ExtractOpenAIContent(dataContent);
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
            var url = new Uri(BaseUrl, "chat/completions");
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);

            var body = new
            {
                model = ModelName,
                messages = new[] { new { role = "user", content = "Hi" } },
                max_tokens = 5
            };

            request.Content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json");

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            var response = await HttpClient.SendAsync(request, cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// DeepSeek provider — OpenAI-compatible with specific defaults.
/// </summary>
public class DeepSeekProvider : OpenAIProvider
{
    public override string ProviderName => "DeepSeek";

    public DeepSeekProvider(string apiKey, string modelName = "deepseek-chat",
        string? baseUrl = null)
        : base(apiKey, baseUrl ?? "https://api.deepseek.com/v1", modelName) { }
}

/// <summary>
/// Qwen (通义千问) provider — OpenAI-compatible with specific defaults.
/// </summary>
public class QwenProvider : OpenAIProvider
{
    public override string ProviderName => "通义千问";

    public QwenProvider(string apiKey, string modelName = "qwen-plus",
        string? baseUrl = null)
        : base(apiKey, baseUrl ?? "https://dashscope.aliyuncs.com/compatible-mode/v1", modelName) { }
}

/// <summary>
/// Google Gemini provider — uses OpenAI-compatible endpoint.
/// </summary>
public class GeminiProvider : OpenAIProvider
{
    public override string ProviderName => "Google Gemini";

    public GeminiProvider(string apiKey, string modelName = "gemini-2.5-flash",
        string? baseUrl = null)
        : base(apiKey, baseUrl ?? "https://generativelanguage.googleapis.com/v1beta/openai", modelName) { }
}

/// <summary>
/// Kimi (Moonshot AI) provider — OpenAI-compatible.
/// </summary>
public class KimiProvider : OpenAIProvider
{
    public override string ProviderName => "Kimi";

    public KimiProvider(string apiKey, string modelName = "moonshot-v1-32k",
        string? baseUrl = null)
        : base(apiKey, baseUrl ?? "https://api.moonshot.cn/v1", modelName) { }
}

/// <summary>
/// 智谱 GLM provider — OpenAI-compatible.
/// </summary>
public class GLMProvider : OpenAIProvider
{
    public override string ProviderName => "智谱 GLM";

    public GLMProvider(string apiKey, string modelName = "glm-4-flash",
        string? baseUrl = null)
        : base(apiKey, baseUrl ?? "https://open.bigmodel.cn/api/paas/v4", modelName) { }
}

/// <summary>
/// MiniMax provider — OpenAI-compatible.
/// </summary>
public class MiniMaxProvider : OpenAIProvider
{
    public override string ProviderName => "MiniMax";

    public MiniMaxProvider(string apiKey, string modelName = "MiniMax-Text-01",
        string? baseUrl = null)
        : base(apiKey, baseUrl ?? "https://api.minimax.chat/v1", modelName) { }
}
