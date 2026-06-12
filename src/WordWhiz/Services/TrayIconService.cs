using H.NotifyIcon;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace WordWhiz.Services;

/// <summary>
/// System tray icon service using H.NotifyIcon.WinUI.
/// </summary>
public class TrayIconService : IDisposable
{
    private TaskbarIcon? _trayIcon;

    /// <summary>
    /// Events for tray menu actions.
    /// </summary>
    public event Action? TriggerOptimizationRequested;
    public event Action? OpenSettingsRequested;
    public event Action? ExitRequested;

    /// <summary>
    /// Initialize and show the system tray icon.
    /// </summary>
    public void Initialize()
    {
        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "WordWhiz - 智能文案优化工具",
            MenuActivation = PopupActivationMode.RightClick,
            IconSource = new BitmapImage(new Uri("ms-appx:///Assets/Square44x44Logo.png")),
        };

        // Create context menu
        var menuFlyout = new MenuFlyout();

        // Title item (disabled)
        var titleItem = new MenuFlyoutItem { Text = "WordWhiz", IsEnabled = false };
        titleItem.Icon = new FontIcon { Glyph = "\uE8A5" };
        menuFlyout.Items.Add(titleItem);

        menuFlyout.Items.Add(new MenuFlyoutSeparator());

        // Optimize clipboard
        var optimizeItem = new MenuFlyoutItem { Text = "优化剪贴板内容" };
        optimizeItem.Click += (_, _) => TriggerOptimizationRequested?.Invoke();
        menuFlyout.Items.Add(optimizeItem);

        // Settings
        var settingsItem = new MenuFlyoutItem { Text = "设置" };
        settingsItem.Click += (_, _) => OpenSettingsRequested?.Invoke();
        menuFlyout.Items.Add(settingsItem);

        menuFlyout.Items.Add(new MenuFlyoutSeparator());

        // Exit
        var exitItem = new MenuFlyoutItem { Text = "退出 WordWhiz" };
        exitItem.Click += (_, _) => ExitRequested?.Invoke();
        menuFlyout.Items.Add(exitItem);

        _trayIcon.ContextFlyout = menuFlyout;

        // Left click triggers optimization
        _trayIcon.LeftClick += (_, _) => TriggerOptimizationRequested?.Invoke();
    }

    /// <summary>
    /// Show the tray icon.
    /// </summary>
    public void Show()
    {
        if (_trayIcon != null)
        {
            _trayIcon.ForceCreate(enablesDesignTime: false);
        }
    }

    /// <summary>
    /// Show a notification tooltip.
    /// </summary>
    public void ShowNotification(string title, string message)
    {
        _trayIcon?.ShowNotification(title, message);
    }

    /// <summary>
    /// Hide the tray icon.
    /// </summary>
    public void Hide()
    {
        _trayIcon?.Dispose();
    }

    public void Dispose()
    {
        _trayIcon?.Dispose();
        _trayIcon = null;
    }
}
