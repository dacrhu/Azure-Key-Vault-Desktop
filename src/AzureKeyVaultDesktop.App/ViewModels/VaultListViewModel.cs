using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia.Threading;
using AzureKeyVaultDesktop.App.Services;
using AzureKeyVaultDesktop.App.Services.Localization;
using AzureKeyVaultDesktop.Core.Models;
using AzureKeyVaultDesktop.Core.Services.Auth;
using AzureKeyVaultDesktop.Core.Services.KeyVault;
using AzureKeyVaultDesktop.Core.Services.Updates;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AzureKeyVaultDesktop.App.ViewModels;

public partial class VaultListViewModel : ViewModelBase
{
    private readonly IKeyVaultManagementService _keyVaultManagementService;
    private readonly IAuthService _authService;
    private readonly IUpdateCheckService _updateCheckService;
    private readonly INavigationService _navigationService;
    private readonly ILocalizationService _loc;

    private string? _updateReleaseUrl;

    [ObservableProperty]
    private bool _updateAvailable;

    [ObservableProperty]
    private string? _updateMessage;

    // Instances are transient (a fresh one per navigation), so reading the current translation
    // once here is enough — it can't go stale mid-visit without a full re-navigation anyway.
    private string AllSubscriptionsLabel => _loc["VaultList_AllSubscriptions"];

    // Raw, unfiltered accumulation of everything the streaming load has produced so far;
    // DisplayedVaults is always recomputed from this plus the current search/subscription filter.
    private readonly List<VaultSummary> _allVaults = new();

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _noResultsVisible;

    [ObservableProperty]
    private string? _searchText;

    [ObservableProperty]
    private string _selectedSubscription;

    public ObservableCollection<string> Subscriptions { get; } = new();

    public ObservableCollection<VaultListItem> DisplayedVaults { get; } = new();

    [ObservableProperty]
    private VaultListItem? _selectedItem;

    public VaultListViewModel(
        IKeyVaultManagementService keyVaultManagementService,
        IAuthService authService,
        IUpdateCheckService updateCheckService,
        INavigationService navigationService,
        ILocalizationService loc)
    {
        _keyVaultManagementService = keyVaultManagementService;
        _authService = authService;
        _updateCheckService = updateCheckService;
        _navigationService = navigationService;
        _loc = loc;

        _selectedSubscription = AllSubscriptionsLabel;
        Subscriptions.Add(AllSubscriptionsLabel);

        _ = InitializeAsync();
        _ = CheckForUpdateAsync();
    }

    private async Task InitializeAsync()
    {
        // First paint: instant if a previous run left an on-disk cache (nothing shown yet, so
        // there's nothing to preserve — a full build is fine here), otherwise this itself is
        // already the live fetch.
        await LoadAsync(forceRefresh: false);

        // Only the disk-cache-served case still needs to check Azure — a genuine live fetch
        // above already flips this off, so this never double-fetches on a cold cache.
        if (_keyVaultManagementService.NeedsStartupReconcile)
        {
            await ReconcileAsync();
        }
    }

    private async Task CheckForUpdateAsync()
    {
        var result = await _updateCheckService.CheckForUpdateAsync(AppVersion.Current);
        if (result is null)
        {
            return;
        }

        _updateReleaseUrl = result.ReleaseUrl;
        UpdateMessage = _loc.Translate("VaultList_UpdateAvailable", result.LatestVersion);
        UpdateAvailable = true;
    }

    [RelayCommand]
    private void OpenReleasePage()
    {
        if (_updateReleaseUrl is null)
        {
            return;
        }

        Process.Start(new ProcessStartInfo(_updateReleaseUrl) { UseShellExecute = true });
    }

    [RelayCommand]
    private Task RefreshAsync() => ReconcileAsync();

    private async Task LoadAsync(bool forceRefresh)
    {
        IsBusy = true;
        StatusMessage = null;
        _allVaults.Clear();
        Subscriptions.Clear();
        Subscriptions.Add(AllSubscriptionsLabel);
        SelectedSubscription = AllSubscriptionsLabel;
        ApplyFilter();
        try
        {
            await foreach (var vault in _keyVaultManagementService.ListAccessibleVaultsAsync(
                forceRefresh,
                onSubscriptionWarning: (sub, reason) =>
                    Dispatcher.UIThread.Post(() => StatusMessage = _loc.Translate("VaultList_SubscriptionWarning", sub, reason))))
            {
                _allVaults.Add(vault);
                EnsureSubscriptionKnown(vault.SubscriptionDisplayName);
                ApplyFilter();
            }

            if (_allVaults.Count == 0 && StatusMessage is null)
            {
                StatusMessage = _loc["VaultList_NoVaultsFound"];
            }
        }
        catch (Exception ex)
        {
            StatusMessage = _loc.Translate("VaultList_LoadError", ex.Message);
        }
        finally
        {
            IsBusy = false;
            ApplyFilter();
        }
    }

    /// <summary>Live-fetches against Azure like <see cref="LoadAsync"/>, but merges into
    /// <see cref="_allVaults"/> instead of clearing it first — vaults still present are left
    /// untouched (just refreshed in place), newly-found ones are added, and ones no longer
    /// accessible are removed. Used both for the once-per-process startup reconcile (so a
    /// relaunch doesn't have to wipe and rebuild the list a disk cache just painted instantly)
    /// and for the manual Refresh button, so neither flashes the list empty.</summary>
    private async Task ReconcileAsync()
    {
        IsBusy = true;
        StatusMessage = null;
        var seenUris = new HashSet<Uri>();
        try
        {
            await foreach (var vault in _keyVaultManagementService.ListAccessibleVaultsAsync(
                forceRefresh: true,
                onSubscriptionWarning: (sub, reason) =>
                    Dispatcher.UIThread.Post(() => StatusMessage = _loc.Translate("VaultList_SubscriptionWarning", sub, reason))))
            {
                seenUris.Add(vault.VaultUri);
                var index = _allVaults.FindIndex(v => v.VaultUri == vault.VaultUri);
                if (index >= 0)
                {
                    _allVaults[index] = vault;
                }
                else
                {
                    _allVaults.Add(vault);
                    EnsureSubscriptionKnown(vault.SubscriptionDisplayName);
                }

                ApplyFilter();
            }

            _allVaults.RemoveAll(v => !seenUris.Contains(v.VaultUri));

            if (_allVaults.Count == 0 && StatusMessage is null)
            {
                StatusMessage = _loc["VaultList_NoVaultsFound"];
            }
        }
        catch (Exception ex)
        {
            StatusMessage = _loc.Translate("VaultList_LoadError", ex.Message);
        }
        finally
        {
            IsBusy = false;
            ApplyFilter();
        }
    }

    partial void OnSearchTextChanged(string? value) => ApplyFilter();

    partial void OnSelectedSubscriptionChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        IEnumerable<VaultSummary> query = _allVaults;

        if (!string.IsNullOrEmpty(SelectedSubscription) && SelectedSubscription != AllSubscriptionsLabel)
        {
            query = query.Where(v => v.SubscriptionDisplayName == SelectedSubscription);
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            query = query.Where(v => v.Name.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        var sorted = query
            .OrderBy(v => v.SubscriptionDisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(v => v.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        DisplayedVaults.Clear();
        string? lastGroup = null;
        foreach (var vault in sorted)
        {
            var isNewGroup = vault.SubscriptionDisplayName != lastGroup;
            DisplayedVaults.Add(new VaultListItem(vault, isNewGroup, vault.SubscriptionDisplayName));
            lastGroup = vault.SubscriptionDisplayName;
        }

        NoResultsVisible = !IsBusy && DisplayedVaults.Count == 0 && _allVaults.Count > 0;
    }

    private void EnsureSubscriptionKnown(string subscriptionName)
    {
        if (Subscriptions.Contains(subscriptionName))
        {
            return;
        }

        var insertIndex = Subscriptions.Count;
        for (var i = 1; i < Subscriptions.Count; i++)
        {
            if (string.Compare(subscriptionName, Subscriptions[i], StringComparison.OrdinalIgnoreCase) < 0)
            {
                insertIndex = i;
                break;
            }
        }

        Subscriptions.Insert(insertIndex, subscriptionName);
    }

    partial void OnSelectedItemChanged(VaultListItem? value)
    {
        if (value is null)
        {
            return;
        }

        var vault = value.Vault;
        SelectedItem = null;
        _navigationService.NavigateTo<SecretListViewModel>(vm => vm.Initialize(vault));
    }

    [RelayCommand]
    private void OpenSettings() => _navigationService.NavigateTo<SettingsViewModel>();

    [RelayCommand]
    private async Task SignOutAsync()
    {
        await _authService.SignOutAsync();
        _navigationService.NavigateTo<LoginViewModel>();
    }
}
