using AzureKeyVaultDesktop.App.Services;
using AzureKeyVaultDesktop.App.Services.Localization;
using AzureKeyVaultDesktop.Core.Models;
using AzureKeyVaultDesktop.Core.Services.KeyVault;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AzureKeyVaultDesktop.App.ViewModels;

public partial class NewSecretViewModel : ViewModelBase
{
    private readonly IKeyVaultSecretsService _secretsService;
    private readonly INavigationService _navigationService;
    private readonly ILocalizationService _loc;

    private VaultSummary? _vault;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _value = string.Empty;

    [ObservableProperty]
    private string _contentType = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _statusMessage;

    public NewSecretViewModel(IKeyVaultSecretsService secretsService, INavigationService navigationService, ILocalizationService loc)
    {
        _secretsService = secretsService;
        _navigationService = navigationService;
        _loc = loc;
    }

    public void Initialize(VaultSummary vault)
    {
        _vault = vault;
        Name = string.Empty;
        Value = string.Empty;
        ContentType = string.Empty;
        StatusMessage = null;
    }

    [RelayCommand]
    private async Task CreateAsync()
    {
        if (_vault is null)
        {
            return;
        }

        if (!SecretNameValidator.IsValid(Name))
        {
            StatusMessage = _loc["NewSecret_InvalidName"];
            return;
        }

        if (string.IsNullOrEmpty(Value))
        {
            StatusMessage = _loc["NewSecret_MissingValue"];
            return;
        }

        IsBusy = true;
        StatusMessage = null;
        try
        {
            var contentType = string.IsNullOrWhiteSpace(ContentType) ? null : ContentType.Trim();
            await _secretsService.CreateSecretAsync(_vault.VaultUri, Name, Value, contentType);
            var vault = _vault;
            _navigationService.NavigateTo<SecretListViewModel>(vm => vm.Initialize(vault));
        }
        catch (Exception ex)
        {
            StatusMessage = _loc.Translate("NewSecret_CreateError", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        var vault = _vault;
        _navigationService.NavigateTo<SecretListViewModel>(vm => vm.Initialize(vault!));
    }
}
