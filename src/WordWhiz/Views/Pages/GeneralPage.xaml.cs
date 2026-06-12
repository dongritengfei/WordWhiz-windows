using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WordWhiz.Models;
using WordWhiz.ViewModels;

namespace WordWhiz.Views.Pages;

public sealed partial class GeneralPage : Page
{
    public SettingsViewModel VM { get; }
    public int PanelPositionIndex
    {
        get => (int)VM.PanelPosition;
        set => VM.PanelPosition = (PanelPosition)value;
    }

    public GeneralPage()
    {
        VM = (SettingsViewModel)((App)Application.Current).GetSettingsViewModel()!;
        InitializeComponent();
    }

    private async void Setting_Changed(object sender, RoutedEventArgs e)
    {
        await VM.SaveGeneralSettingsAsync();
    }

    private async void PanelPosition_Changed(object sender, SelectionChangedEventArgs e)
    {
        VM.PanelPosition = (PanelPosition)PanelPositionIndex;
        await VM.SaveGeneralSettingsAsync();
    }
}
