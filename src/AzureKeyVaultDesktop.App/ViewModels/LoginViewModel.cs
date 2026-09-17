using AzureKeyVaultDesktop.App.Services;
using AzureKeyVaultDesktop.App.Services.Localization;
using AzureKeyVaultDesktop.Core.Services.Auth;
using AzureKeyVaultDesktop.Core.Services.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AzureKeyVaultDesktop.App.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    // If the tenant rejects the sign-in (e.g. the user isn't a member of the app's
    // organization), the browser shows an error page and never redirects back to the app —
    // without a timeout, the interactive credential call would then hang forever with no
    // error. This bounds that wait and lets SignInAsync fail with a clear message instead.
    private static readonly TimeSpan SignInTimeout = TimeSpan.FromMinutes(3);

    private readonly ISettingsService _settingsService;
    private readonly IAuthService _authService;
    private readonly INavigationService _navigationService;
    private readonly ILocalizationService _loc;

    private CancellationTokenSource? _signInCts;

    [ObservableProperty]
    private string? _clientId;

    [ObservableProperty]
    private string? _tenantId;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _statusMessage;

    public LoginViewModel(
        ISettingsService settingsService,
        IAuthService authService,
        INavigationService navigationService,
        ILocalizationService loc)
    {
        _settingsService = settingsService;
        _authService = authService;
        _navigationService = navigationService;
        _loc = loc;

        _clientId = settingsService.Current.ClientId;
        _tenantId = settingsService.Current.TenantId;

        _ = TryRestoreSessionAsync();
    }

    private async Task TryRestoreSessionAsync()
    {
        if (string.IsNullOrWhiteSpace(_settingsService.Current.ClientId))
        {
            return;
        }

        IsBusy = true;
        StatusMessage = _loc["Login_RestoringSession"];
        try
        {
            if (await _authService.TryRestoreSessionAsync())
            {
                _navigationService.NavigateTo<VaultListViewModel>();
                return;
            }

            StatusMessage = null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SignInAsync()
    {
        if (string.IsNullOrWhiteSpace(ClientId))
        {
            StatusMessage = _loc["Login_MissingClientId"];
            return;
        }

        _settingsService.Current.ClientId = ClientId.Trim();
        _settingsService.Current.TenantId = string.IsNullOrWhiteSpace(TenantId) ? null : TenantId.Trim();
        await _settingsService.SaveAsync();

        IsBusy = true;
        StatusMessage = _loc["Login_SigningIn"];

        _signInCts = new CancellationTokenSource(SignInTimeout);
        try
        {
            await _authService.SignInInteractiveAsync(_signInCts.Token);
            _navigationService.NavigateTo<VaultListViewModel>();
        }
        catch (OperationCanceledException)
        {
            StatusMessage = _loc["Login_SignInCancelledOrTimedOut"];
        }
        catch (Exception ex)
        {
            StatusMessage = _loc.Translate("Login_SignInFailed", ex.Message);
        }
        finally
        {
            IsBusy = false;
            _signInCts?.Dispose();
            _signInCts = null;
        }
    }

    [RelayCommand]
    private void CancelSignIn() => _signInCts?.Cancel();

    [RelayCommand]
    private void OpenSetupGuide() => _navigationService.NavigateTo<SetupGuideViewModel>();
}
