using System.Reflection;
using AzureKeyVaultDesktop.App.Services;
using AzureKeyVaultDesktop.App.Services.Localization;
using AzureKeyVaultDesktop.Core.Services.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AzureKeyVaultDesktop.App.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly INavigationService _navigationService;
    private readonly ILocalizationService _loc;

    public string AppVersion { get; } = GetAppVersion();

    [ObservableProperty]
    private string? _clientId;

    [ObservableProperty]
    private string? _tenantId;

    [ObservableProperty]
    private int _clipboardAutoClearSeconds;

    [ObservableProperty]
    private string? _statusMessage;

    public IReadOnlyList<LanguageOption> AvailableLanguages { get; }

    [ObservableProperty]
    private LanguageOption _selectedLanguage;

    public SettingsViewModel(ISettingsService settingsService, INavigationService navigationService, ILocalizationService loc)
    {
        _settingsService = settingsService;
        _navigationService = navigationService;
        _loc = loc;

        _clientId = settingsService.Current.ClientId;
        _tenantId = settingsService.Current.TenantId;
        _clipboardAutoClearSeconds = settingsService.Current.ClipboardAutoClearSeconds;

        AvailableLanguages = _loc.AvailableLanguages.Select(l => new LanguageOption(l.Code, l.DisplayName)).ToList();
        _selectedLanguage = AvailableLanguages.FirstOrDefault(l => l.Code == _loc.CurrentLanguage) ?? AvailableLanguages[0];
    }

    partial void OnSelectedLanguageChanged(LanguageOption value) => _loc.CurrentLanguage = value.Code;

    [RelayCommand]
    private async Task SaveAsync()
    {
        _settingsService.Current.ClientId = string.IsNullOrWhiteSpace(ClientId) ? null : ClientId.Trim();
        _settingsService.Current.TenantId = string.IsNullOrWhiteSpace(TenantId) ? null : TenantId.Trim();
        _settingsService.Current.ClipboardAutoClearSeconds =
            ClipboardAutoClearSeconds > 0 ? ClipboardAutoClearSeconds : 30;
        _settingsService.Current.Language = SelectedLanguage.Code;

        await _settingsService.SaveAsync();
        StatusMessage = _loc["Settings_Saved"];
    }

    [RelayCommand]
    private void Back() => _navigationService.NavigateTo<VaultListViewModel>();

    private static string GetAppVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "dev";
    }
}
