using WordWhiz.Models;

namespace WordWhiz.Services.LLM;

/// <summary>
/// Factory for creating LLM provider instances based on configuration.
/// </summary>
public class LLMProviderFactory
{
    /// <summary>
    /// Create a provider instance based on the given configuration.
    /// </summary>
    public ILLMProvider? CreateProvider(LLMProviderConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.ApiKey))
            return null;

        var baseUrl = !string.IsNullOrWhiteSpace(config.BaseUrl)
            ? config.BaseUrl
            : LLMProviderConfig.GetDefaultBaseURL(config.Provider);

        var model = !string.IsNullOrWhiteSpace(config.Model)
            ? config.Model
            : LLMProviderConfig.GetDefaultModel(config.Provider);

        return config.Provider switch
        {
            LLMProviderType.OpenAI => new OpenAIProvider(config.ApiKey, baseUrl, model),
            LLMProviderType.Anthropic => new AnthropicProvider(config.ApiKey, model),
            LLMProviderType.DeepSeek => new DeepSeekProvider(config.ApiKey, model),
            LLMProviderType.Qwen => new QwenProvider(config.ApiKey, model),
            LLMProviderType.Custom => !string.IsNullOrWhiteSpace(baseUrl)
                ? new OpenAIProvider(config.ApiKey, baseUrl, model)
                : null,
            _ => null
        };
    }

    /// <summary>
    /// Get all supported provider types.
    /// </summary>
    public static IEnumerable<LLMProviderType> GetSupportedProviders()
    {
        return Enum.GetValues<LLMProviderType>();
    }
}
