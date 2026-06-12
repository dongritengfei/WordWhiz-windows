using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WordWhiz.Models;
using WordWhiz.ViewModels;

namespace WordWhiz.Views.Pages;

public sealed partial class PromptsPage : Page
{
    public SettingsViewModel VM { get; }

    public PromptsPage()
    {
        VM = (SettingsViewModel)((App)Application.Current).GetSettingsViewModel()!;
        InitializeComponent();
    }

    private void Prompt_Selected(object sender, SelectionChangedEventArgs e)
    {
        if (VM.SelectedPromptItem != null)
        {
            EditPanel.Visibility = Visibility.Visible;
            AddButton.Visibility = Visibility.Collapsed;
            NameBox.Text = VM.SelectedPromptItem.Name;
            TemplateBox.Text = VM.SelectedPromptItem.PromptTemplate;
        }
        else
        {
            EditPanel.Visibility = Visibility.Collapsed;
            AddButton.Visibility = Visibility.Visible;
        }
    }

    private async void AddPrompt_Click(object sender, RoutedEventArgs e)
    {
        await VM.AddPromptAsync();
    }

    private async void DeletePrompt_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is CustomPrompt prompt)
        {
            await VM.DeletePromptAsync(prompt);
            EditPanel.Visibility = Visibility.Collapsed;
            AddButton.Visibility = Visibility.Visible;
        }
    }

    private async void SavePrompt_Click(object sender, RoutedEventArgs e)
    {
        if (VM.SelectedPromptItem != null)
        {
            VM.SelectedPromptItem.Name = NameBox.Text;
            VM.SelectedPromptItem.PromptTemplate = TemplateBox.Text;
            await VM.SavePromptAsync(VM.SelectedPromptItem);
        }
    }
}
