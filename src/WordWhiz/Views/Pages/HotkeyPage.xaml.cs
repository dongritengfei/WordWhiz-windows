using Microsoft.UI.Xaml.Controls;
using WordWhiz.ViewModels;

namespace WordWhiz.Views.Pages;

public sealed partial class HotkeyPage : Page
{
    public SettingsViewModel VM { get; }

    public HotkeyPage()
    {
        VM = (SettingsViewModel)((App)Microsoft.UI.Xaml.Application.Current).GetSettingsViewModel()!;
        InitializeComponent();
    }
}
