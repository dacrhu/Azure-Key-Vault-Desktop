using System.Collections.ObjectModel;
using AzureKeyVaultDesktop.App.Services;
using AzureKeyVaultDesktop.App.Services.Localization;
using AzureKeyVaultDesktop.Core.Models;
using AzureKeyVaultDesktop.Core.Services.KeyVault;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AzureKeyVaultDesktop.App.ViewModels;

public partial class SecretListViewModel : ViewModelBase
{
    private readonly IKeyVaultSecretsService _secretsService;
    private readonly INavigationService _navigationService;
    private readonly ILocalizationService _loc;

    private VaultSummary? _vault;

    // Raw, unfiltered accumulation of everything the streaming load has produced so far;
    // DisplayedSecrets is always recomputed from this plus the current search text.
    private readonly List<SecretSummary> _allSecrets = new();

    [ObservableProperty]
    private string _vaultName = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _noResultsVisible;

    [ObservableProperty]
    private string? _searchText;

    public ObservableCollection<SecretSummary> DisplayedSecrets { get; } = new();

    [ObservableProperty]
    private SecretSummary? _selectedSecret;

    public SecretListViewModel(IKeyVaultSecretsService secretsService, INavigationService navigationService, ILocalizationService loc)
    {
        _secretsService = secretsService;
        _navigationService = navigationService;
        _loc = loc;
    }

    public void Initialize(VaultSummary vault)
    {
        _vault = vault;
        VaultName = vault.Name;
        _ = LoadAsync(forceRefresh: false);
    }

    [RelayCommand]
    private Task RefreshAsync() => LoadAsync(forceRefresh: true);

    private async Task LoadAsync(bool forceRefresh)
    {
        if (_vault is null)
        {
            return;
        }

        IsBusy = true;
        StatusMessage = null;
        _allSecrets.Clear();
        ApplyFilter();
        try
        {
            await foreach (var secret in _secretsService.ListSecretsAsync(_vault.VaultUri, forceRefresh))
            {
                _allSecrets.Add(secret);
                ApplyFilter();
            }

            if (_allSecrets.Count == 0)
            {
                StatusMessage = _loc["SecretList_NoSecrets"];
            }
        }
        catch (Exception ex)
        {
            StatusMessage = _loc.Translate("SecretList_LoadError", ex.Message);
        }
        finally
        {
            IsBusy = false;
            ApplyFilter();
        }
    }

    partial void OnSearchTextChanged(string? value) => ApplyFilter();

    private void ApplyFilter()
    {
        IEnumerable<SecretSummary> query = _allSecrets;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            query = query.Where(s => s.Name.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        var sorted = query.OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase).ToList();

        DisplayedSecrets.Clear();
        foreach (var secret in sorted)
        {
            DisplayedSecrets.Add(secret);
        }

        NoResultsVisible = !IsBusy && DisplayedSecrets.Count == 0 && _allSecrets.Count > 0;
    }

    partial void OnSelectedSecretChanged(SecretSummary? value)
    {
        if (value is null || _vault is null)
        {
            return;
        }

        var vault = _vault;
        _navigationService.NavigateTo<SecretDetailViewModel>(vm => vm.Initialize(vault, value.Name));
    }

    [RelayCommand]
    private void NewSecret()
    {
        if (_vault is null)
        {
            return;
        }

        _navigationService.NavigateTo<NewSecretViewModel>(vm => vm.Initialize(_vault));
    }

    [RelayCommand]
    private void Back() => _navigationService.NavigateTo<VaultListViewModel>();
}
