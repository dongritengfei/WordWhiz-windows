using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;
using WordWhiz.Helpers;
using WordWhiz.Models;
using WordWhiz.Views;

namespace WordWhiz.Services;

/// <summary>
/// Manages the floating optimization panel window.
/// Uses Win32 API to create a borderless, topmost, tool-style window.
/// </summary>
public class PanelWindowService
{
    private Window? _panelWindow;
    private IntPtr _hwnd;
    private bool _isVisible;

    // Win32 constants
    private const int GWL_STYLE = -16;
    private const int GWL_EXSTYLE = -20;
    private const uint WS_CAPTION = 0x00C00000;
    private const uint WS_THICKFRAME = 0x00040000;
    private const uint WS_MINIMIZEBOX = 0x00020000;
    private const uint WS_MAXIMIZEBOX = 0x00010000;
    private const uint WS_SYSMENU = 0x00080000;
    private const uint WS_EX_TOOLWINDOW = 0x00000080;
    private const uint WS_EX_TOPMOST = 0x00000008;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_SHOWWINDOW = 0x0040;

    [DllImport("user32.dll")]
    private static extern uint GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern uint SetWindowLongPtr(IntPtr hWnd, int nIndex, uint dwNewLong);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    private static readonly IntPtr HWND_TOPMOST = new(-1);

    private const int SW_SHOW = 5;
    private const int SW_HIDE = 0;

    /// <summary>
    /// Whether the panel is currently visible.
    /// </summary>
    public bool IsVisible => _isVisible;

    /// <summary>
    /// Event raised when the panel is closed/hidden.
    /// </summary>
    public event Action? PanelClosed;

    /// <summary>
    /// Show the optimization panel with the given source text.
    /// </summary>
    public void ShowPanel(string sourceText, PanelPosition position = PanelPosition.ScreenRight)
    {
        if (_panelWindow == null)
        {
            CreatePanelWindow();
        }

        // Update the view model
        if (_panelWindow?.Content is FrameworkElement root &&
            root.DataContext is ViewModels.PanelViewModel vm)
        {
            vm.SourceText = sourceText;
            vm.LoadPrompts();
            vm.Optimize();
        }

        // Position the window
        PositionWindow(position);

        // Show and bring to front
        ShowWindow(_hwnd, SW_SHOW);
        SetWindowPos(_hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOACTIVATE | SWP_SHOWWINDOW);
        SetForegroundWindow(_hwnd);

        _isVisible = true;
    }

    /// <summary>
    /// Hide the optimization panel.
    /// </summary>
    public void HidePanel()
    {
        if (_hwnd != IntPtr.Zero && _isVisible)
        {
            ShowWindow(_hwnd, SW_HIDE);
            _isVisible = false;
            PanelClosed?.Invoke();
        }
    }

    /// <summary>
    /// Toggle panel visibility.
    /// </summary>
    public void TogglePanel(string sourceText, PanelPosition position = PanelPosition.ScreenRight)
    {
        if (_isVisible)
        {
            HidePanel();
        }
        else
        {
            ShowPanel(sourceText, position);
        }
    }

    private void CreatePanelWindow()
    {
        _panelWindow = new Window
        {
            Title = "WordWhiz"
        };

        // Set content
        _panelWindow.Content = new OptimizationPanel();

        // Get window handle
        _hwnd = WindowNative.GetWindowHandle(_panelWindow);

        // Configure window styles
        ConfigureWindowStyles();

        // Configure AppWindow
        var windowId = Win32Interop.GetWindowIdFromWindow(_hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);

        // Remove title bar and set size
        appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;

        // Set window size
        appWindow.Resize(new Windows.Graphics.SizeInt32(Constants.PanelWidth, Constants.PanelHeight));

        // Hide from taskbar and Alt+Tab
        appWindow.IsShownInSwitchers = false;

        // Handle window closed
        _panelWindow.Closed += (_, _) =>
        {
            _isVisible = false;
            PanelClosed?.Invoke();
        };
    }

    private void ConfigureWindowStyles()
    {
        // Remove standard window chrome
        var style = GetWindowLongPtr(_hwnd, GWL_STYLE);
        style &= ~(WS_CAPTION | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX | WS_SYSMENU);
        SetWindowLongPtr(_hwnd, GWL_STYLE, style);

        // Add tool window and topmost extended styles
        var exStyle = GetWindowLongPtr(_hwnd, GWL_EXSTYLE);
        exStyle |= WS_EX_TOOLWINDOW | WS_EX_TOPMOST;
        SetWindowLongPtr(_hwnd, GWL_EXSTYLE, exStyle);
    }

    private void PositionWindow(PanelPosition position)
    {
        var screenWidth = GetScreenWidth();
        var screenHeight = GetScreenHeight();

        var x = position switch
        {
            PanelPosition.ScreenRight => screenWidth - Constants.PanelWidth - 20,
            PanelPosition.ScreenLeft => 20,
            PanelPosition.ScreenCenter => (screenWidth - Constants.PanelWidth) / 2,
            _ => screenWidth - Constants.PanelWidth - 20
        };

        var y = (screenHeight - Constants.PanelHeight) / 2;

        SetWindowPos(_hwnd, IntPtr.Zero, x, Math.Max(y, 0), Constants.PanelWidth, Constants.PanelHeight,
            SWP_NOACTIVATE | SWP_SHOWWINDOW);
    }

    private static int GetScreenWidth()
    {
        var displayArea = DisplayArea.Primary;
        return displayArea.WorkArea.Width;
    }

    private static int GetScreenHeight()
    {
        var displayArea = DisplayArea.Primary;
        return displayArea.WorkArea.Height;
    }
}
