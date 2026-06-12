using Microsoft.UI.Xaml;
using WordWhiz.Helpers;
using WordWhiz.Models;
using WordWhiz.Services;
using WordWhiz.Services.Interfaces;
using WordWhiz.Services.LLM;
using WordWhiz.ViewModels;
using WordWhiz.Views;

namespace WordWhiz;

/// <summary>
/// Application entry point and lifecycle manager.
/// </summary>
public partial class App : Application
{
    private MainWindow? _mainWindow;
    private HotkeyService? _hotkeyService;
    private TrayIconService? _trayIconService;
    private PanelWindowService? _panelWindowService;
    private ClipboardService? _clipboardService;
    private DataService? _dataService;
    private SecureStorageService? _secureStorageService;
    private LLMProviderFactory? _providerFactory;
    private PanelViewModel? _panelViewModel;
    private SettingsViewModel? _settingsViewModel;

    public static App? Current { get; private set; }

    public App()
    {
        InitializeComponent();
        Current = this;
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Initialize data layer
        _dataService = new DataService();
        await _dataService.InitializeAsync();

        _secureStorageService = new SecureStorageService(_dataService);
        _providerFactory = new LLMProviderFactory();
        _clipboardService = new ClipboardService();

        // Create ViewModels
        _panelViewModel = new PanelViewModel(_dataService, _secureStorageService, _providerFactory, _clipboardService);
        _settingsViewModel = new SettingsViewModel(_dataService, _secureStorageService, _providerFactory);

        // Create main window (hidden host)
        _mainWindow = new MainWindow();
        _mainWindow.Activate();

        // Initialize system services
        _hotkeyService = new HotkeyService();
        _hotkeyService.Initialize(_mainWindow.Hwnd);

        _panelWindowService = new PanelWindowService();

        _trayIconService = new TrayIconService();

        // Wire up events
        _hotkeyService.OnTriggerHotkey += HandleTriggerHotkey;
        _hotkeyService.OnSettingsHotkey += HandleSettingsHotkey;

        _trayIconService.TriggerOptimizationRequested += HandleTriggerHotkey;
        _trayIconService.OpenSettingsRequested += HandleSettingsHotkey;
        _trayIconService.ExitRequested += HandleExit;

        // Register default hotkeys
        _hotkeyService.RegisterDefaults();

        // Initialize and show tray icon
        _trayIconService.Initialize();
        _trayIconService.Show();

        // Hide main window
        _mainWindow.HideWindow();

        // Check if onboarding is needed
        var hasCompletedOnboarding = await _dataService.GetSettingAsync(Constants.HasCompletedOnboardingKey, false);
        if (!hasCompletedOnboarding)
        {
            await Task.Delay(500);
            ShowOnboarding();
        }
    }

    private async void HandleTriggerHotkey()
    {
        if (_clipboardService == null || _panelWindowService == null) return;

        var text = await _clipboardService.ReadTextAsync();
        if (string.IsNullOrWhiteSpace(text))
        {
            _trayIconService?.ShowNotification("WordWhiz", "剪贴板中没有文本内容，请先复制需要优化的文本");
            return;
        }

        var position = await _dataService!.GetSettingAsync(Constants.PanelPositionKey, PanelPosition.ScreenRight);
        _panelWindowService.ShowPanel(text, position);
    }

    private void HandleSettingsHotkey()
    {
        ShowSettings();
    }

    private void HandleExit()
    {
        _hotkeyService?.Dispose();
        _trayIconService?.Dispose();
        _dataService?.Dispose();
        Exit();
    }

    public void ShowSettings()
    {
        if (_settingsViewModel == null) return;

        var settingsWindow = new SettingsWindow(_settingsViewModel);
        settingsWindow.Activate();
    }

    public void ShowOnboarding()
    {
        if (_settingsViewModel == null) return;

        var onboardingWindow = new OnboardingWindow(_settingsViewModel, _dataService!);
        onboardingWindow.Activate();
    }

    public PanelViewModel? GetPanelViewModel() => _panelViewModel;
    public SettingsViewModel? GetSettingsViewModel() => _settingsViewModel;
    public IDataService? GetDataService() => _dataService;
    public PanelWindowService? GetPanelWindowService() => _panelWindowService;
    public HotkeyService? GetHotkeyService() => _hotkeyService;
    public TrayIconService? GetTrayIconService() => _trayIconService;
    public ClipboardService? GetClipboardService() => _clipboardService;
}
