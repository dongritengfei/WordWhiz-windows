using Microsoft.UI.Xaml;
using WinRT.Interop;
using WordWhiz.Models;
using WordWhiz.Services;
using WordWhiz.Services.Interfaces;
using WordWhiz.Services.LLM;
using WordWhiz.ViewModels;

namespace WordWhiz.Views;

/// <summary>
/// Onboarding wizard window for first-time setup.
/// </summary>
public sealed partial class OnboardingWindow : Window
{
    private readonly SettingsViewModel _settingsVm;
    private readonly IDataService _dataService;
    private int _currentStep;

    public OnboardingWindow(SettingsViewModel settingsVm, IDataService dataService)
    {
        _settingsVm = settingsVm;
        _dataService = dataService;
        InitializeComponent();

        // Configure window
        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
        appWindow.Resize(new Windows.Graphics.SizeInt32(560, 520));

        // Default provider selection
        ProviderCombo.SelectedIndex = 3; // Qwen
    }

    private void UpdateStep()
    {
        Step1.Visibility = _currentStep == 0 ? Visibility.Visible : Visibility.Collapsed;
        Step2.Visibility = _currentStep == 1 ? Visibility.Visible : Visibility.Collapsed;
        Step3.Visibility = _currentStep == 2 ? Visibility.Visible : Visibility.Collapsed;

        BackButton.Visibility = _currentStep > 0 ? Visibility.Visible : Visibility.Collapsed;
        NextButton.Content = _currentStep == 2 ? "开始使用 🚀" : "下一步 →";
    }

    private void Next_Click(object sender, RoutedEventArgs e)
    {
        if (_currentStep < 2)
        {
            _currentStep++;
            UpdateStep();
        }
        else
        {
            // Complete onboarding
            _ = CompleteOnboardingAsync();
        }
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        if (_currentStep > 0)
        {
            _currentStep--;
            UpdateStep();
        }
    }

    private async void TestConnection_Click(object sender, RoutedEventArgs e)
    {
        TestResult.Text = "测试中...";

        var providerIndex = ProviderCombo.SelectedIndex;
        var providerType = (LLMProviderType)providerIndex;
        var apiKey = ApiKeyBox.Password;

        if (string.IsNullOrEmpty(apiKey))
        {
            TestResult.Text = "请先输入 API Key";
            return;
        }

        try
        {
            var config = new LLMProviderConfig
            {
                Provider = providerType,
                ApiKey = apiKey,
                BaseUrl = LLMProviderConfig.GetDefaultBaseURL(providerType),
                Model = LLMProviderConfig.GetDefaultModel(providerType)
            };

            var factory = new LLMProviderFactory();
            var provider = factory.CreateProvider(config);
            if (provider == null)
            {
                TestResult.Text = "配置无效";
                return;
            }

            var success = await provider.TestConnectionAsync();
            TestResult.Text = success ? "连接成功 ✓" : "连接失败 ✗";
        }
        catch (Exception ex)
        {
            TestResult.Text = $"错误: {ex.Message}";
        }
    }

    private async Task CompleteOnboardingAsync()
    {
        // Save API settings
        var providerIndex = ProviderCombo.SelectedIndex;
        var providerType = (LLMProviderType)providerIndex;

        _settingsVm.SelectedProvider = providerType;
        _settingsVm.ApiKey = ApiKeyBox.Password;
        _settingsVm.BaseUrl = LLMProviderConfig.GetDefaultBaseURL(providerType);
        _settingsVm.ModelName = LLMProviderConfig.GetDefaultModel(providerType);

        await _settingsVm.SaveApiSettingsAsync();

        // Mark onboarding complete
        await _dataService.SetSettingAsync(Helpers.Constants.HasCompletedOnboardingKey, true);

        // Close window
        Close();
    }
}
