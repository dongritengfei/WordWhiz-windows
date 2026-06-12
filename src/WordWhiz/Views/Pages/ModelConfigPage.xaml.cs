using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WordWhiz.Models;
using WordWhiz.ViewModels;

namespace WordWhiz.Views.Pages;

public sealed partial class ModelConfigPage : Page
{
    public SettingsViewModel VM { get; }

    public ModelConfigPage()
    {
        VM = (SettingsViewModel)((App)Application.Current).GetSettingsViewModel()!;
        InitializeComponent();

        // Set initial provider selection
        ProviderCombo.SelectedIndex = (int)VM.SelectedProvider;
        ApiKeyBox.Password = VM.ApiKey;

        VM.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SettingsViewModel.ConnectionTestResult))
            {
                TestResult.Text = VM.ConnectionTestResult;
            }
        };
    }

    private async void Provider_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (ProviderCombo.SelectedIndex >= 0)
        {
            VM.SelectedProvider = (LLMProviderType)ProviderCombo.SelectedIndex;
            VM.BaseUrl = LLMProviderConfig.GetDefaultBaseURL(VM.SelectedProvider);
            VM.ModelName = LLMProviderConfig.GetDefaultModel(VM.SelectedProvider);
            BaseUrlBox.Text = VM.BaseUrl;
            ModelBox.Text = VM.ModelName;

            // Reload API key for this provider
            var apiKeyKey = $"apikey.{VM.SelectedProvider}";
            var dataService = App.Current!.GetDataService()!;
            var secureStorage = new Services.SecureStorageService(dataService);
            ApiKeyBox.Password = await secureStorage.RetrieveAsync(apiKeyKey) ?? "";
            VM.ApiKey = ApiKeyBox.Password;

            await VM.SaveApiSettingsAsync();
        }
    }

    private async void ApiKey_Changed(object sender, RoutedEventArgs e)
    {
        VM.ApiKey = ApiKeyBox.Password;
        await VM.SaveApiSettingsAsync();
    }

    private async void ApiSetting_Changed(object sender, RoutedEventArgs e)
    {
        await VM.SaveApiSettingsAsync();
    }

    private async void TestConnection_Click(object sender, RoutedEventArgs e)
    {
        await VM.TestConnectionAsync();
    }
}
