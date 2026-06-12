using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WordWhiz.Helpers;
using WordWhiz.Models;
using WordWhiz.Services;
using WordWhiz.Services.Interfaces;
using WordWhiz.Services.LLM;

namespace WordWhiz.ViewModels;

/// <summary>
/// ViewModel for the onboarding wizard.
/// </summary>
public partial class OnboardingViewModel : ObservableObject
{
    private readonly SettingsViewModel _settingsVm;
    private readonly IDataService _dataService;

    [ObservableProperty] private int _currentStep;
    [ObservableProperty] private string _apiKey = string.Empty;
    [ObservableProperty] private LLMProviderType _selectedProvider = LLMProviderType.Qwen;
    [ObservableProperty] private bool _isTestingConnection;
    [ObservableProperty] private string? _connectionTestResult;

    public bool CanGoNext => CurrentStep < 2;
    public bool CanGoBack => CurrentStep > 0;

    public List<LLMProviderType> ProviderTypes { get; } =
    [
        LLMProviderType.OpenAI,
        LLMProviderType.Anthropic,
        LLMProviderType.DeepSeek,
        LLMProviderType.Qwen,
        LLMProviderType.Custom
    ];

    public OnboardingViewModel(SettingsViewModel settingsVm, IDataService dataService)
    {
        _settingsVm = settingsVm;
        _dataService = dataService;
    }

    [RelayCommand]
    public void NextStep()
    {
        if (CurrentStep < 2)
        {
            CurrentStep++;
            OnPropertyChanged(nameof(CanGoNext));
            OnPropertyChanged(nameof(CanGoBack));
        }
    }

    [RelayCommand]
    public void PreviousStep()
    {
        if (CurrentStep > 0)
        {
            CurrentStep--;
            OnPropertyChanged(nameof(CanGoNext));
            OnPropertyChanged(nameof(CanGoBack));
        }
    }

    [RelayCommand]
    public async Task TestConnectionAsync()
    {
        IsTestingConnection = true;
        ConnectionTestResult = null;

        try
        {
            var config = new LLMProviderConfig
            {
                Provider = SelectedProvider,
                ApiKey = ApiKey,
                BaseUrl = LLMProviderConfig.GetDefaultBaseURL(SelectedProvider),
                Model = LLMProviderConfig.GetDefaultModel(SelectedProvider)
            };

            var factory = new LLMProviderFactory();
            var provider = factory.CreateProvider(config);
            if (provider == null)
            {
                ConnectionTestResult = "API Key 未配置";
                return;
            }

            var success = await provider.TestConnectionAsync();
            ConnectionTestResult = success ? "连接成功 ✓" : "连接失败";
        }
        catch (Exception ex)
        {
            ConnectionTestResult = $"错误: {ex.Message}";
        }
        finally
        {
            IsTestingConnection = false;
        }
    }

    [RelayCommand]
    public async Task CompleteAsync()
    {
        // Save API settings
        _settingsVm.SelectedProvider = SelectedProvider;
        _settingsVm.ApiKey = ApiKey;
        _settingsVm.BaseUrl = LLMProviderConfig.GetDefaultBaseURL(SelectedProvider);
        _settingsVm.ModelName = LLMProviderConfig.GetDefaultModel(SelectedProvider);

        await _settingsVm.SaveApiSettingsAsync();

        // Mark onboarding as complete
        await _dataService.SetSettingAsync(Constants.HasCompletedOnboardingKey, true);
    }
}
