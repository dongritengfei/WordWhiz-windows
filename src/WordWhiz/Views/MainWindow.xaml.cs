using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace WordWhiz.Views;

/// <summary>
/// Hidden host window for system tray and hotkey message loop.
/// </summary>
public sealed partial class MainWindow : Window
{
    public IntPtr Hwnd { get; private set; }

    public MainWindow()
    {
        InitializeComponent();
        Hwnd = WindowNative.GetWindowHandle(this);

        // Hide from taskbar and Alt+Tab
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(Hwnd);
        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
        appWindow.IsShownInSwitchers = false;

        // Minimize window size (it's hidden anyway)
        appWindow.Resize(new Windows.Graphics.SizeInt32(1, 1));
    }

    /// <summary>
    /// Hide the main window (keep running in background).
    /// </summary>
    public void HideWindow()
    {
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(Hwnd);
        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
        appWindow.Hide();
    }
}
