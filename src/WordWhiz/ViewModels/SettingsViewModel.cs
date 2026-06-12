using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using WordWhiz.Helpers;
using WordWhiz.Models;
using WordWhiz.Services;
using WordWhiz.Services.Interfaces;
using WordWhiz.Services.LLM;

namespace WordWhiz.ViewModels;

/// <summary>
/// ViewModel for the settings window.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly IDataService _dataService;
    private readonly ISecureStorageService _secureStorage;
    private readonly LLMProviderFactory _providerFactory;
    private readonly DispatcherQueue _dispatcher;

    // ── General Settings ─────────────────────────────────────
    [ObservableProperty] private bool _launchAtStartup = true;
    [ObservableProperty] private PanelPosition _panelPosition = PanelPosition.ScreenRight;
    [ObservableProperty] private bool _autoCopy = true;
    [ObservableProperty] private bool _keepHistory = true;
    [ObservableProperty] private int _maxHistoryRecords = 50;

    // ── API Settings ─────────────────────────────────────────
    [ObservableProperty] private LLMProviderType _selectedProvider = LLMProviderType.Qwen;
    [ObservableProperty] private string _apiKey = string.Empty;
    [ObservableProperty] private string _baseUrl = string.Empty;
    [ObservableProperty] private string _modelName = string.Empty;
    [ObservableProperty] private bool _isTestingConnection;
    [ObservableProperty] private string? _connectionTestResult;

    // ── Prompts ──────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<CustomPrompt> _prompts = [];
    [ObservableProperty] private CustomPrompt? _selectedPromptItem;

    // ── History ──────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<OptimizationRecord> _historyRecords = [];
    [ObservableProperty] private string _historySearchQuery = string.Empty;
    [ObservableProperty] private OptimizationRecord? _selectedHistoryRecord;

    // ── Hotkeys ──────────────────────────────────────────────
    [ObservableProperty] private string _triggerHotkeyDisplay = "Ctrl+Shift+Z";
    [ObservableProperty] private ObservableCollection<HotkeyConfig> _hotkeyPresets = [];

    // ── Navigation ───────────────────────────────────────────
    [ObservableProperty] private SettingsTab _selectedTab = SettingsTab.General;

    // Provider list for ComboBox
    public List<LLMProviderType> ProviderTypes { get; } =
    [
        LLMProviderType.OpenAI,
        LLMProviderType.Anthropic,
        LLMProviderType.DeepSeek,
        LLMProviderType.Qwen,
        LLMProviderType.Gemini,
        LLMProviderType.Kimi,
        LLMProviderType.GLM,
        LLMProviderType.MiniMax,
        LLMProviderType.Custom
    ];

    public SettingsViewModel(
        IDataService dataService,
        ISecureStorageService secureStorage,
        LLMProviderFactory providerFactory)
    {
        _dataService = dataService;
        _secureStorage = secureStorage;
        _providerFactory = providerFactory;
        _dispatcher = DispatcherQueue.GetForCurrentThread();
        HotkeyPresets = new ObservableCollection<HotkeyConfig>(HotkeyConfig.TriggerPresets);
    }

    public async Task LoadSettingsAsync()
    {
        LaunchAtStartup = await _dataService.GetSettingAsync(Constants.LaunchAtLoginKey, true);
        PanelPosition = await _dataService.GetSettingAsync(Constants.PanelPositionKey, PanelPosition.ScreenRight);
        AutoCopy = await _dataService.GetSettingAsync(Constants.AutoCopyKey, true);
        KeepHistory = await _dataService.GetSettingAsync(Constants.KeepHistoryKey, true);
        MaxHistoryRecords = await _dataService.GetSettingAsync(Constants.MaxHistoryRecordsKey, Constants.DefaultMaxHistoryRecords);

        SelectedProvider = await _dataService.GetSettingAsync(Constants.LLMProviderKey, LLMProviderType.Qwen);
        BaseUrl = await _dataService.GetSettingAsync(Constants.ApiBaseURLKey, "");
        ModelName = await _dataService.GetSettingAsync(Constants.ModelNameKey, "");

        var apiKeyKey = $"apikey.{SelectedProvider}";
        ApiKey = await _secureStorage.RetrieveAsync(apiKeyKey) ?? "";

        await LoadPromptsAsync();
        await LoadHistoryAsync();

        var hotkeyService = App.Current?.GetHotkeyService();
        TriggerHotkeyDisplay = hotkeyService?.GetTriggerDisplay() ?? "Ctrl+Shift+Z";
    }

    // ── Save Settings ────────────────────────────────────────

    public async Task SaveGeneralSettingsAsync()
    {
        await _dataService.SetSettingAsync(Constants.LaunchAtLoginKey, LaunchAtStartup);
        await _dataService.SetSettingAsync(Constants.PanelPositionKey, PanelPosition);
        await _dataService.SetSettingAsync(Constants.AutoCopyKey, AutoCopy);
        await _dataService.SetSettingAsync(Constants.KeepHistoryKey, KeepHistory);
        await _dataService.SetSettingAsync(Constants.MaxHistoryRecordsKey, MaxHistoryRecords);
    }

    public async Task SaveApiSettingsAsync()
    {
        await _dataService.SetSettingAsync(Constants.LLMProviderKey, SelectedProvider);
        await _dataService.SetSettingAsync(Constants.ApiBaseURLKey, BaseUrl);
        await _dataService.SetSettingAsync(Constants.ModelNameKey, ModelName);

        var apiKeyKey = $"apikey.{SelectedProvider}";
        if (!string.IsNullOrEmpty(ApiKey))
        {
            await _secureStorage.StoreAsync(apiKeyKey, ApiKey);
        }
        else
        {
            await _secureStorage.DeleteAsync(apiKeyKey);
        }
    }

    // ── Provider Selection Changed ───────────────────────────

    partial void OnSelectedProviderChanged(LLMProviderType value)
    {
        // Always pre-fill default URL and model when switching providers
        BaseUrl = LLMProviderConfig.GetDefaultBaseURL(value);
        ModelName = LLMProviderConfig.GetDefaultModel(value);
    }

    // ── Test Connection ──────────────────────────────────────

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
                BaseUrl = BaseUrl,
                Model = ModelName
            };

            var provider = _providerFactory.CreateProvider(config);
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

    // ── Prompts ──────────────────────────────────────────────

    public async Task LoadPromptsAsync()
    {
        var prompts = await _dataService.GetPromptsAsync();
        _dispatcher.TryEnqueue(() =>
        {
            Prompts = new ObservableCollection<CustomPrompt>(prompts);
        });
    }

    [RelayCommand]
    public async Task AddPromptAsync()
    {
        var prompt = new CustomPrompt
        {
            Name = "新指令",
            PromptTemplate = "你是一位专业的文案助手。请按照以下指令处理文本：\n\n需处理的文本：{{text}}",
            SortOrder = Prompts.Count
        };
        await _dataService.InsertPromptAsync(prompt);
        await LoadPromptsAsync();
    }

    [RelayCommand]
    public async Task DeletePromptAsync(CustomPrompt prompt)
    {
        await _dataService.DeletePromptAsync(prompt.Id);
        await LoadPromptsAsync();
    }

    [RelayCommand]
    public async Task SavePromptAsync(CustomPrompt prompt)
    {
        await _dataService.UpdatePromptAsync(prompt);
    }

    // ── History ──────────────────────────────────────────────

    public async Task LoadHistoryAsync()
    {
        List<OptimizationRecord> records;
        if (string.IsNullOrWhiteSpace(HistorySearchQuery))
        {
            records = await _dataService.GetRecordsAsync();
        }
        else
        {
            records = await _dataService.SearchRecordsAsync(HistorySearchQuery);
        }

        _dispatcher.TryEnqueue(() =>
        {
            HistoryRecords = new ObservableCollection<OptimizationRecord>(records);
        });
    }

    [RelayCommand]
    public async Task DeleteHistoryRecordAsync(OptimizationRecord record)
    {
        await _dataService.DeleteRecordAsync(record.Id);
        await LoadHistoryAsync();
    }

    [RelayCommand]
    public async Task ClearAllHistoryAsync()
    {
        await _dataService.ClearAllRecordsAsync();
        await LoadHistoryAsync();
    }

    [RelayCommand]
    public async Task SearchHistoryAsync()
    {
        await LoadHistoryAsync();
    }
}
