using System.Collections.ObjectModel;
using Avalonia.Threading;
using AzureKeyVaultDesktop.App.Services;
using AzureKeyVaultDesktop.App.Services.Localization;
using AzureKeyVaultDesktop.Core.Models;
using AzureKeyVaultDesktop.Core.Services.Clipboard;
using AzureKeyVaultDesktop.Core.Services.KeyVault;
using AzureKeyVaultDesktop.Core.Services.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AzureKeyVaultDesktop.App.ViewModels;

public partial class SecretDetailViewModel : ViewModelBase
{
    private readonly IKeyVaultSecretsService _secretsService;
    private readonly IClipboardService _clipboardService;
    private readonly ISettingsService _settingsService;
    private readonly INavigationService _navigationService;
    private readonly ILocalizationService _loc;

    private VaultSummary? _vault;

    [ObservableProperty]
    private string _secretName = string.Empty;

    [ObservableProperty]
    private string? _revealedValue;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string _editedValue = string.Empty;

    public ObservableCollection<SecretVersionSummary> Versions { get; } = new();

    [ObservableProperty]
    private SecretVersionSummary? _selectedVersion;

    public SecretDetailViewModel(
        IKeyVaultSecretsService secretsService,
        IClipboardService clipboardService,
        ISettingsService settingsService,
        INavigationService navigationService,
        ILocalizationService loc)
    {
        _secretsService = secretsService;
        _clipboardService = clipboardService;
        _settingsService = settingsService;
        _navigationService = navigationService;
        _loc = loc;

        _clipboardService.CountdownTick += (_, seconds) => Dispatcher.UIThread.Post(() =>
            StatusMessage = seconds > 0
                ? _loc.Translate("SecretDetail_CopiedCountdown", seconds)
                : _loc["SecretDetail_ClipboardCleared"]);
    }

    public void Initialize(VaultSummary vault, string secretName)
    {
        _vault = vault;
        SecretName = secretName;
        RevealedValue = null;
        StatusMessage = null;
        IsEditing = false;
        Versions.Clear();

        _ = LoadVersionsAsync();
    }

    private async Task LoadVersionsAsync()
    {
        if (_vault is null)
        {
            return;
        }

        try
        {
            var versions = new List<SecretVersionSummary>();
            await foreach (var version in _secretsService.ListSecretVersionsAsync(_vault.VaultUri, SecretName))
            {
                versions.Add(version);
            }

            Versions.Clear();
            foreach (var version in versions.OrderByDescending(v => v.UpdatedOn))
            {
                Versions.Add(version);
            }

            SelectedVersion = Versions.FirstOrDefault(v => v.IsCurrent) ?? Versions.FirstOrDefault();
        }
        catch (Exception ex)
        {
            StatusMessage = _loc.Translate("SecretDetail_VersionsError", ex.Message);
        }
    }

    partial void OnSelectedVersionChanged(SecretVersionSummary? value)
    {
        // Only auto-refetch if the user has already explicitly revealed a value this visit —
        // otherwise switching the version dropdown before ever clicking Reveal would silently
        // pull a secret value without an explicit action, which is exactly what Reveal exists
        // to prevent.
        if (RevealedValue is not null)
        {
            _ = RevealAsync();
        }
    }

    [RelayCommand]
    private async Task RevealAsync()
    {
        if (_vault is null)
        {
            return;
        }

        IsBusy = true;
        StatusMessage = null;
        try
        {
            var version = SelectedVersion is { IsCurrent: false } ? SelectedVersion.Version : null;
            RevealedValue = await _secretsService.GetSecretValueAsync(_vault.VaultUri, SecretName, version);
        }
        catch (Exception ex)
        {
            StatusMessage = _loc.Translate("SecretDetail_RevealError", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CopyAsync()
    {
        if (RevealedValue is null)
        {
            await RevealAsync();
        }

        if (RevealedValue is null)
        {
            return;
        }

        var timeout = TimeSpan.FromSeconds(Math.Max(1, _settingsService.Current.ClipboardAutoClearSeconds));
        await _clipboardService.CopySecretAsync(RevealedValue, timeout);
        StatusMessage = _loc.Translate("SecretDetail_CopiedCountdown", (int)timeout.TotalSeconds);
    }

    [RelayCommand]
    private void BeginEdit()
    {
        IsEditing = true;
        EditedValue = RevealedValue ?? string.Empty;
    }

    [RelayCommand]
    private void CancelEdit() => IsEditing = false;

    [RelayCommand]
    private async Task SaveNewVersionAsync()
    {
        if (_vault is null)
        {
            return;
        }

        IsBusy = true;
        StatusMessage = null;
        try
        {
            await _secretsService.UpdateSecretValueAsync(_vault.VaultUri, SecretName, EditedValue);
            RevealedValue = EditedValue;
            IsEditing = false;
            StatusMessage = _loc["SecretDetail_NewVersionSaved"];
            await LoadVersionsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = _loc.Translate("SecretDetail_SaveError", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Back()
    {
        var vault = _vault;
        _navigationService.NavigateTo<SecretListViewModel>(vm => vm.Initialize(vault!));
    }
}
