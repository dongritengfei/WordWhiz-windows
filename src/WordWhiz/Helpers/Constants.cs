namespace WordWhiz.Helpers;

/// <summary>
/// Application-wide constants.
/// </summary>
public static class Constants
{
    // Panel dimensions
    public const int PanelWidth = 660;
    public const int PanelHeight = 660;
    public const int PanelCornerRadius = 12;

    // Animation
    public const double SlideAnimationDuration = 0.3;
    public const int SlideAnimationOffset = 60;

    // Network
    public const int RequestTimeoutSeconds = 30;

    // UI
    public const int HeaderHeight = 44;
    public const int SourceMaxLines = 3;
    public const int SourceMaxHeight = 80;

    // Database
    public const string DatabaseFileName = "wordwhiz.db";
    public const string AppFolderName = "WordWhiz";
    public const string DataFolderName = "data";

    // Settings keys
    public const string HasCompletedOnboardingKey = "has_completed_onboarding";
    public const string HotkeyEnabledKey = "hotkey_enabled";
    public const string HotkeyConfigKey = "hotkey_config";
    public const string LaunchAtLoginKey = "launch_at_login";
    public const string AutoCopyKey = "auto_copy";
    public const string KeepHistoryKey = "keep_history";
    public const string PanelPositionKey = "panel_position";
    public const string LLMProviderKey = "llm_provider";
    public const string ApiBaseURLKey = "api_base_url";
    public const string ModelNameKey = "model_name";
    public const string PanelPinnedFrameKey = "panel_pinned_frame";
    public const string MaxHistoryRecordsKey = "max_history_records";

    // Hotkey IDs
    public const int HotkeyIdTrigger = 1;
    public const int HotkeyIdCopy = 2;
    public const int HotkeyIdSettings = 3;

    // Win32 Messages
    public const int WM_HOTKEY = 0x0312;

    // Max history records
    public const int DefaultMaxHistoryRecords = 50;
}
