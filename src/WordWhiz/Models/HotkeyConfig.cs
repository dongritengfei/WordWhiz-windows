using System.Runtime.InteropServices;

namespace WordWhiz.Models;

/// <summary>
/// Win32 hotkey modifier flags.
/// </summary>
[Flags]
public enum HotkeyModifiers : uint
{
    None = 0x0000,
    Alt = 0x0001,       // MOD_ALT
    Control = 0x0002,   // MOD_CONTROL
    Shift = 0x0004,     // MOD_SHIFT
    Win = 0x0008,       // MOD_WIN
    NoRepeat = 0x4000   // MOD_NOREPEAT
}

/// <summary>
/// Represents a configurable keyboard shortcut.
/// </summary>
public class HotkeyConfig
{
    public uint KeyCode { get; set; }
    public HotkeyModifiers Modifiers { get; set; }
    public string DisplayString { get; set; } = string.Empty;

    /// <summary>
    /// Default trigger hotkey: Ctrl+Shift+Z (avoids conflict with system Undo).
    /// </summary>
    public static HotkeyConfig DefaultTrigger => new()
    {
        KeyCode = 0x5A, // VK_Z
        Modifiers = HotkeyModifiers.Control | HotkeyModifiers.Shift | HotkeyModifiers.NoRepeat,
        DisplayString = "Ctrl+Shift+Z"
    };

    /// <summary>
    /// Default copy result hotkey: Ctrl+Shift+C.
    /// </summary>
    public static HotkeyConfig DefaultCopy => new()
    {
        KeyCode = 0x43, // VK_C
        Modifiers = HotkeyModifiers.Control | HotkeyModifiers.Shift | HotkeyModifiers.NoRepeat,
        DisplayString = "Ctrl+Shift+C"
    };

    /// <summary>
    /// Default open settings hotkey: Ctrl+Shift+Comma.
    /// </summary>
    public static HotkeyConfig DefaultSettings => new()
    {
        KeyCode = 0xBC, // VK_OEM_COMMA
        Modifiers = HotkeyModifiers.Control | HotkeyModifiers.Shift | HotkeyModifiers.NoRepeat,
        DisplayString = "Ctrl+Shift+,"
    };

    /// <summary>
    /// All preset hotkey options for the trigger shortcut.
    /// </summary>
    public static List<HotkeyConfig> TriggerPresets =>
    [
        DefaultTrigger,
        new()
        {
            KeyCode = 0x5A, // VK_Z
            Modifiers = HotkeyModifiers.Control | HotkeyModifiers.Alt | HotkeyModifiers.NoRepeat,
            DisplayString = "Ctrl+Alt+Z"
        },
        new()
        {
            KeyCode = 0x20, // VK_SPACE
            Modifiers = HotkeyModifiers.Control | HotkeyModifiers.NoRepeat,
            DisplayString = "Ctrl+Space"
        },
        new()
        {
            KeyCode = 0x20, // VK_SPACE
            Modifiers = HotkeyModifiers.Control | HotkeyModifiers.Shift | HotkeyModifiers.NoRepeat,
            DisplayString = "Ctrl+Shift+Space"
        },
        new()
        {
            KeyCode = 0x4F, // VK_O
            Modifiers = HotkeyModifiers.Control | HotkeyModifiers.Alt | HotkeyModifiers.NoRepeat,
            DisplayString = "Ctrl+Alt+O"
        }
    ];
}

/// <summary>
/// Panel position on screen.
/// </summary>
public enum PanelPosition
{
    ScreenRight,
    ScreenLeft,
    ScreenCenter
}

/// <summary>
/// Streaming status of the optimization panel.
/// </summary>
public enum StreamingStatus
{
    Idle,
    Streaming,
    Complete,
    Stopped,
    Error
}

/// <summary>
/// Settings window tab selection.
/// </summary>
public enum SettingsTab
{
    General,
    ModelConfig,
    Prompts,
    History,
    Hotkeys,
    About
}
