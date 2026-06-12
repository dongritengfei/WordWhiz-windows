using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WordWhiz.Models;
using WordWhiz.ViewModels;

namespace WordWhiz.Views.Pages;

public sealed partial class HistoryPage : Page
{
    public SettingsViewModel VM { get; }

    public HistoryPage()
    {
        VM = (SettingsViewModel)((App)Application.Current).GetSettingsViewModel()!;
        InitializeComponent();
        _ = VM.LoadHistoryAsync();
    }

    private async void Search_Submitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        VM.HistorySearchQuery = args.QueryText;
        await VM.SearchHistoryAsync();
    }

    private async void DeleteRecord_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is OptimizationRecord record)
        {
            await VM.DeleteHistoryRecordAsync(record);
        }
    }

    private async void ClearAll_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "确认清空",
            Content = "确定要清空所有历史记录吗？此操作不可撤销。",
            PrimaryButtonText = "清空",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await VM.ClearAllHistoryAsync();
        }
    }
}
