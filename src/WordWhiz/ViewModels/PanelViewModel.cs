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
/// ViewModel for the optimization panel.
/// </summary>
public partial class PanelViewModel : ObservableObject
{
    private readonly IDataService _dataService;
    private readonly ISecureStorageService _secureStorage;
    private readonly LLMProviderFactory _providerFactory;
    private readonly ClipboardService _clipboardService;
    private readonly DispatcherQueue _dispatcher;

    [ObservableProperty] private string _sourceText = string.Empty;
    [ObservableProperty] private string _resultText = string.Empty;
    [ObservableProperty] private CustomPrompt? _selectedPrompt;
    [ObservableProperty] private ObservableCollection<CustomPrompt> _prompts = [];
    [ObservableProperty] private StreamingStatus _status = StreamingStatus.Idle;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _isSourceExpanded = true;
    [ObservableProperty] private bool _isPinned;
    [ObservableProperty] private bool _showCopiedFeedback;
    [ObservableProperty] private string _hotkeyDisplay = "Ctrl+Shift+Z";

    private CancellationTokenSource? _optimizationCts;

    public int ResultCharacterCount => ResultText.Length;
    public bool IsStreaming => Status == StreamingStatus.Streaming;

    public string StatusLabel => Status switch
    {
        StreamingStatus.Idle => "",
        StreamingStatus.Streaming => "● 生成中...",
        StreamingStatus.Complete => "✓ 生成完成",
        StreamingStatus.Stopped => "⏸ 已停止",
        StreamingStatus.Error => $"⚠ {ErrorMessage}",
        _ => ""
    };

    public PanelViewModel(
        IDataService dataService,
        ISecureStorageService secureStorage,
        LLMProviderFactory providerFactory,
        ClipboardService clipboardService)
    {
        _dataService = dataService;
        _secureStorage = secureStorage;
        _providerFactory = providerFactory;
        _clipboardService = clipboardService;
        _dispatcher = DispatcherQueue.GetForCurrentThread();
    }

    public async void LoadPrompts()
    {
        var prompts = await _dataService.GetPromptsAsync();
        _dispatcher.TryEnqueue(() =>
        {
            Prompts = new ObservableCollection<CustomPrompt>(prompts);
            if (SelectedPrompt == null && Prompts.Count > 0)
            {
                SelectedPrompt = Prompts[0];
            }
        });
    }

    [RelayCommand]
    public async Task OptimizeAsync()
    {
        var text = SourceText.Trim();
        if (string.IsNullOrEmpty(text)) return;

        // Cancel any existing streaming
        StopStreaming();

        // Reset state
        ResultText = "";
        Status = StreamingStatus.Streaming;
        ErrorMessage = string.Empty;
        OnPropertyChanged(nameof(StatusLabel));
        OnPropertyChanged(nameof(IsStreaming));

        _optimizationCts = new CancellationTokenSource();
        var ct = _optimizationCts.Token;

        try
        {
            var provider = await CreateCurrentProviderAsync();
            if (provider == null)
            {
                Status = StreamingStatus.Error;
                ErrorMessage = "请先配置 API Key";
                OnPropertyChanged(nameof(StatusLabel));
                OnPropertyChanged(nameof(IsStreaming));
                return;
            }

            var (systemPrompt, userPrompt) = BuildPrompts(text);

            await foreach (var token in provider.StreamOptimizeAsync(systemPrompt, userPrompt, ct))
            {
                _dispatcher.TryEnqueue(() =>
                {
                    ResultText += token;
                    OnPropertyChanged(nameof(ResultCharacterCount));
                });
            }

            if (!ct.IsCancellationRequested)
            {
                _dispatcher.TryEnqueue(() =>
                {
                    Status = StreamingStatus.Complete;
                    OnPropertyChanged(nameof(StatusLabel));
                    OnPropertyChanged(nameof(IsStreaming));
                });
                await SaveRecordAsync();

                // Auto-copy if setting enabled
                var autoCopy = await _dataService.GetSettingAsync(Constants.AutoCopyKey, true);
                if (autoCopy)
                {
                    _dispatcher.TryEnqueue(() => _clipboardService.WriteText(ResultText));
                }
            }
        }
        catch (OperationCanceledException)
        {
            _dispatcher.TryEnqueue(() =>
            {
                Status = StreamingStatus.Stopped;
                OnPropertyChanged(nameof(StatusLabel));
                OnPropertyChanged(nameof(IsStreaming));
            });
        }
        catch (Exception ex)
        {
            _dispatcher.TryEnqueue(() =>
            {
                Status = StreamingStatus.Error;
                ErrorMessage = ex.Message;
                OnPropertyChanged(nameof(StatusLabel));
                OnPropertyChanged(nameof(IsStreaming));
            });
        }
    }

    [RelayCommand]
    public void StopStreaming()
    {
        _optimizationCts?.Cancel();
        _optimizationCts = null;
        if (Status == StreamingStatus.Streaming)
        {
            Status = StreamingStatus.Stopped;
            OnPropertyChanged(nameof(StatusLabel));
            OnPropertyChanged(nameof(IsStreaming));
        }
    }

    [RelayCommand]
    public async Task RegenerateAsync()
    {
        await OptimizeAsync();
    }

    [RelayCommand]
    public void SwitchPrompt(CustomPrompt prompt)
    {
        SelectedPrompt = prompt;

        if (Status == StreamingStatus.Streaming)
        {
            StopStreaming();
        }

        if (!string.IsNullOrEmpty(SourceText))
        {
            _ = OptimizeAsync();
        }
    }

    [RelayCommand]
    public void CopyResult()
    {
        if (string.IsNullOrEmpty(ResultText)) return;

        _clipboardService.WriteText(ResultText);
        ShowCopiedFeedback = true;

        Task.Run(async () =>
        {
            await Task.Delay(1500);
            _dispatcher.TryEnqueue(() => ShowCopiedFeedback = false);
        });
    }

    [RelayCommand]
    public void ToggleSourceExpanded()
    {
        IsSourceExpanded = !IsSourceExpanded;
    }

    [RelayCommand]
    public void ClosePanel()
    {
        App.Current?.GetPanelWindowService()?.HidePanel();
    }

    // ── Private ──────────────────────────────────────────────

    private async Task<ILLMProvider?> CreateCurrentProviderAsync()
    {
        var providerType = await _dataService.GetSettingAsync(Constants.LLMProviderKey, LLMProviderType.Qwen);
        var apiKeyKey = $"apikey.{providerType}";
        var apiKey = await _secureStorage.RetrieveAsync(apiKeyKey);

        if (string.IsNullOrEmpty(apiKey)) return null;

        var baseUrl = await _dataService.GetSettingAsync(Constants.ApiBaseURLKey, "");
        var model = await _dataService.GetSettingAsync(Constants.ModelNameKey, "");

        var config = new LLMProviderConfig
        {
            Provider = providerType,
            ApiKey = apiKey,
            BaseUrl = baseUrl,
            Model = model
        };

        return _providerFactory.CreateProvider(config);
    }

    private (string systemPrompt, string userPrompt) BuildPrompts(string sourceText)
    {
        if (SelectedPrompt != null)
        {
            var template = SelectedPrompt.PromptTemplate;
            var separator = "\n\n需处理的文本：";
            var parts = template.Split(separator, 2);

            if (parts.Length == 2)
            {
                return (parts[0], parts[1].Replace("{{text}}", sourceText));
            }

            // Fallback
            return (
                "你是一位专业的文案助手。请按照以下指令处理文本，输出仅包含处理后的文本。",
                template.Replace("{{text}}", sourceText)
            );
        }

        return (
            "你是一位专业的文案编辑。请对用户提供的文本进行润色优化，修正语法错误、改善措辞表达、提升文字质量，但保持原文核心意思不变。输出仅包含优化后的文本，不需要解释修改原因。",
            sourceText
        );
    }

    private async Task SaveRecordAsync()
    {
        if (string.IsNullOrEmpty(SourceText) || string.IsNullOrEmpty(ResultText)) return;

        var record = new OptimizationRecord
        {
            SourceText = SourceText,
            ResultText = ResultText,
            PromptName = SelectedPrompt?.Name,
            CharacterCount = ResultText.Length
        };

        await _dataService.InsertRecordAsync(record);

        // Cleanup old records
        var maxRecords = await _dataService.GetSettingAsync(Constants.MaxHistoryRecordsKey, Constants.DefaultMaxHistoryRecords);
        var count = await _dataService.GetRecordCountAsync();
        if (count > maxRecords)
        {
            var toDelete = await _dataService.GetRecordsAsync(count - maxRecords, maxRecords);
            foreach (var r in toDelete)
            {
                await _dataService.DeleteRecordAsync(r.Id);
            }
        }
    }
}
