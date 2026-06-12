using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WordWhiz.Models;
using WordWhiz.ViewModels;

namespace WordWhiz.Views;

/// <summary>
/// Code-behind for the optimization panel.
/// </summary>
public sealed partial class OptimizationPanel : Page
{
    public PanelViewModel ViewModel { get; }

    public OptimizationPanel()
    {
        ViewModel = App.Current!.GetPanelViewModel()!;
        InitializeComponent();
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(PanelViewModel.Status):
                UpdateStatusUI();
                break;
            case nameof(PanelViewModel.ShowCopiedFeedback):
                CopyButton.Content = ViewModel.ShowCopiedFeedback ? "✅ 已复制" : "📋 复制结果";
                break;
        }
    }

    private void UpdateStatusUI()
    {
        StatusText.Text = ViewModel.StatusLabel;
        StatusText.Foreground = ViewModel.Status switch
        {
            StreamingStatus.Streaming => Application.Current.Resources["AccentBrush"] as Microsoft.UI.Xaml.Media.SolidColorBrush,
            StreamingStatus.Complete => Application.Current.Resources["SuccessBrush"] as Microsoft.UI.Xaml.Media.SolidColorBrush,
            StreamingStatus.Stopped => Application.Current.Resources["WarningBrush"] as Microsoft.UI.Xaml.Media.SolidColorBrush,
            StreamingStatus.Error => Application.Current.Resources["ErrorBrush"] as Microsoft.UI.Xaml.Media.SolidColorBrush,
            _ => Application.Current.Resources["TextMutedBrush"] as Microsoft.UI.Xaml.Media.SolidColorBrush
        };

        StopButton.Visibility = ViewModel.IsStreaming ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        App.Current?.ShowSettings();
    }

    private void PinButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.IsPinned = !ViewModel.IsPinned;
        PinButton.Opacity = ViewModel.IsPinned ? 1.0 : 0.5;
    }

    private void ToggleSource_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ToggleSourceExpanded();
        SourceContent.Visibility = ViewModel.IsSourceExpanded ? Visibility.Visible : Visibility.Collapsed;
        CollapseIcon.Text = ViewModel.IsSourceExpanded ? "▼" : "▶";
    }

    private void PromptTab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is CustomPrompt prompt)
        {
            ViewModel.SwitchPrompt(prompt);
        }
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.StopStreaming();
    }

    private void Regenerate_Click(object sender, RoutedEventArgs e)
    {
        _ = ViewModel.RegenerateAsync();
    }

    private void SelectAll_Click(object sender, RoutedEventArgs e)
    {
        ResultEditor.SelectAll();
    }

    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.CopyResult();
    }

    private void ResultEditor_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Auto-scroll to bottom during streaming
        if (ViewModel.IsStreaming && ResultEditor.Text.Length > 0)
        {
            ResultEditor.SelectionStart = ResultEditor.Text.Length;
            ResultEditor.SelectionLength = 0;
        }
    }
}
