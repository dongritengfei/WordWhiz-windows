namespace WordWhiz.Models;

/// <summary>
/// Supported LLM providers and their default configurations.
/// </summary>
public enum LLMProviderType
{
    OpenAI,
    Anthropic,
    DeepSeek,
    Qwen,
    Custom
}

/// <summary>
/// Configuration for an LLM provider instance.
/// </summary>
public class LLMProviderConfig
{
    public LLMProviderType Provider { get; set; } = LLMProviderType.Qwen;
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int MaxTokens { get; set; } = 2000;
    public double Temperature { get; set; } = 0.7;

    public static string GetDisplayName(LLMProviderType provider) => provider switch
    {
        LLMProviderType.OpenAI => "OpenAI (GPT-4o)",
        LLMProviderType.Anthropic => "Anthropic (Claude)",
        LLMProviderType.DeepSeek => "DeepSeek",
        LLMProviderType.Qwen => "通义千问",
        LLMProviderType.Custom => "自定义 (OpenAI 兼容)",
        _ => provider.ToString()
    };

    public static string GetDefaultBaseURL(LLMProviderType provider) => provider switch
    {
        LLMProviderType.OpenAI => "https://api.openai.com/v1",
        LLMProviderType.Anthropic => "https://api.anthropic.com",
        LLMProviderType.DeepSeek => "https://api.deepseek.com/v1",
        LLMProviderType.Qwen => "https://dashscope.aliyuncs.com/compatible-mode/v1",
        LLMProviderType.Custom => "",
        _ => ""
    };

    public static string GetDefaultModel(LLMProviderType provider) => provider switch
    {
        LLMProviderType.OpenAI => "gpt-4o",
        LLMProviderType.Anthropic => "claude-sonnet-4-20250514",
        LLMProviderType.DeepSeek => "deepseek-chat",
        LLMProviderType.Qwen => "qwen-plus",
        LLMProviderType.Custom => "",
        _ => ""
    };
}
