using WordWhiz.Models;

namespace WordWhiz.Helpers;

/// <summary>
/// Default prompt templates seeded on first launch.
/// </summary>
public static class DefaultPrompts
{
    public static List<CustomPrompt> GetDefaults() =>
    [
        new()
        {
            Name = "✨ 润色",
            PromptTemplate = "你是一位专业的中文文案编辑。请对用户提供的文本进行润色优化，修正语法错误、改善措辞表达、提升文字质量，但保持原文核心意思不变。输出仅包含优化后的文本，不需要解释修改原因。\n\n需处理的文本：{{text}}",
            SortOrder = 0
        },
        new()
        {
            Name = "🌍 翻译",
            PromptTemplate = "你是一位专业的翻译专家。请自动检测源语言：如果原文是中文则翻译为英文，如果原文是英文则翻译为中文。输出仅包含翻译后的文本，不需要解释。\n\n需处理的文本：{{text}}",
            SortOrder = 1
        },
        new()
        {
            Name = "📋 摘要",
            PromptTemplate = "你是一位内容摘要专家。请将以下长文本压缩为核心要点，保留关键信息，输出简洁精炼的摘要。输出仅包含摘要文本。\n\n需处理的文本：{{text}}",
            SortOrder = 2
        },
        new()
        {
            Name = "📝 扩写",
            PromptTemplate = "你是一位文案扩写专家。请在保持原意的基础上，丰富细节、增加论据、扩展表述，使内容更加充实完整。输出仅包含扩写后的文本。\n\n需处理的文本：{{text}}",
            SortOrder = 3
        },
        new()
        {
            Name = "👔 正式化",
            PromptTemplate = "你是一位商务写作专家。请将以下文本转换为正式、规范的书面语，适用于商务邮件、官方文件等场景。保持原意不变，语气正式专业。输出仅包含转换后的文本。\n\n需处理的文本：{{text}}",
            SortOrder = 4
        },
        new()
        {
            Name = "💬 口语化",
            PromptTemplate = "你是一位社交媒体文案专家。请将以下文本转换为自然、亲切的口语风格，适用于社交媒体、日常沟通等场景。保持原意不变，语气轻松活泼。输出仅包含转换后的文本。\n\n需处理的文本：{{text}}",
            SortOrder = 5
        }
    ];
}
