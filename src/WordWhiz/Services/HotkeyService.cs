using System.Runtime.InteropServices;
using WordWhiz.Helpers;
using WordWhiz.Models;

namespace WordWhiz.Services;

/// <summary>
/// Global hotkey service using Win32 RegisterHotKey API.
/// </summary>
public class HotkeyService : IDisposable
{
    private IntPtr _hwnd;
    private readonly Dictionary<int, Action> _callbacks = new();
    private readonly Dictionary<int, HotkeyConfig> _registeredHotkeys = new();
    private nint _originalWndProc;
    private WndProcDelegate? _wndProcDelegate;

    // Win32 delegate for WndProc subclassing
    private delegate nint WndProcDelegate(IntPtr hwnd, uint msg, nuint wParam, nint lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    private static extern nint SetWindowLongPtr(IntPtr hWnd, int nIndex, nint dwNewLong);

    [DllImport("user32.dll")]
    private static extern nint GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern nint CallWindowProc(nint lpPrevWndFunc, IntPtr hWnd, uint msg, nuint wParam, nint lParam);

    private const int GWLP_WNDPROC = -4;

    /// <summary>
    /// Event raised when the trigger hotkey is pressed.
    /// </summary>
    public event Action? OnTriggerHotkey;

    /// <summary>
    /// Event raised when the copy hotkey is pressed.
    /// </summary>
    public event Action? OnCopyHotkey;

    /// <summary>
    /// Event raised when the settings hotkey is pressed.
    /// </summary>
    public event Action? OnSettingsHotkey;

    /// <summary>
    /// Initialize the hotkey service with the window handle for message processing.
    /// </summary>
    public void Initialize(IntPtr hwnd)
    {
        _hwnd = hwnd;
        SubclassWindow();
    }

    /// <summary>
    /// Register a global hotkey.
    /// </summary>
    public bool RegisterHotkey(int id, HotkeyConfig config, Action callback)
    {
        // Unregister existing hotkey with this ID first
        UnregisterHotkey(id);

        var modifiers = (uint)config.Modifiers;
        var success = RegisterHotKey(_hwnd, id, modifiers, config.KeyCode);

        if (success)
        {
            _callbacks[id] = callback;
            _registeredHotkeys[id] = config;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Unregister a specific hotkey.
    /// </summary>
    public void UnregisterHotkey(int id)
    {
        if (_registeredHotkeys.ContainsKey(id))
        {
            UnregisterHotKey(_hwnd, id);
            _callbacks.Remove(id);
            _registeredHotkeys.Remove(id);
        }
    }

    /// <summary>
    /// Unregister all hotkeys.
    /// </summary>
    public void UnregisterAll()
    {
        foreach (var id in _registeredHotkeys.Keys.ToList())
        {
            UnregisterHotKey(_hwnd, id);
        }
        _callbacks.Clear();
        _registeredHotkeys.Clear();
    }

    /// <summary>
    /// Register default hotkeys.
    /// </summary>
    public bool RegisterDefaults()
    {
        var triggerOk = RegisterHotkey(Constants.HotkeyIdTrigger, HotkeyConfig.DefaultTrigger,
            () => OnTriggerHotkey?.Invoke());

        var settingsOk = RegisterHotkey(Constants.HotkeyIdSettings, HotkeyConfig.DefaultSettings,
            () => OnSettingsHotkey?.Invoke());

        return triggerOk && settingsOk;
    }

    /// <summary>
    /// Get the display string for the current trigger hotkey.
    /// </summary>
    public string GetTriggerDisplay()
    {
        return _registeredHotkeys.TryGetValue(Constants.HotkeyIdTrigger, out var config)
            ? config.DisplayString
            : HotkeyConfig.DefaultTrigger.DisplayString;
    }

    // ── WndProc Subclassing ─────────────────────────────────

    private void SubclassWindow()
    {
        _wndProcDelegate = WndProc;
        var newProc = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate);
        _originalWndProc = SetWindowLongPtr(_hwnd, GWLP_WNDPROC, newProc);
    }

    private nint WndProc(IntPtr hwnd, uint msg, nuint wParam, nint lParam)
    {
        if (msg == Constants.WM_HOTKEY)
        {
            var id = (int)wParam;
            if (_callbacks.TryGetValue(id, out var callback))
            {
                callback.Invoke();
                return 0;
            }
        }

        return CallWindowProc(_originalWndProc, hwnd, msg, wParam, lParam);
    }

    public void Dispose()
    {
        UnregisterAll();

        // Restore original WndProc
        if (_originalWndProc != nint.Zero)
        {
            SetWindowLongPtr(_hwnd, GWLP_WNDPROC, _originalWndProc);
            _originalWndProc = nint.Zero;
        }
    }
}
