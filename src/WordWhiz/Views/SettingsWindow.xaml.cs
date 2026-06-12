using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinRT.Interop;
using WordWhiz.ViewModels;
using WordWhiz.Views.Pages;

namespace WordWhiz.Views;

/// <summary>
/// Settings window with navigation view.
/// </summary>
public sealed partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;

    public SettingsWindow(SettingsViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();

        // Configure window size
        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
        appWindow.Resize(new Windows.Graphics.SizeInt32(800, 600));

        // Navigate to first page
        SettingsNav.SelectedItem = SettingsNav.MenuItems[0];
        ContentFrame.Navigate(typeof(GeneralPage), _viewModel);

        // Load settings
        _ = _viewModel.LoadSettingsAsync();
    }

    private void SettingsNav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item)
        {
            var tag = item.Tag?.ToString();
            var pageType = tag switch
            {
                "General" => typeof(GeneralPage),
                "ModelConfig" => typeof(ModelConfigPage),
                "Prompts" => typeof(PromptsPage),
                "History" => typeof(HistoryPage),
                "Hotkeys" => typeof(HotkeyPage),
                "About" => typeof(AboutPage),
                _ => typeof(GeneralPage)
            };
            ContentFrame.Navigate(pageType, _viewModel);
        }
    }
}
