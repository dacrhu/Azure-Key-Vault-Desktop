using System.Text.Json;
using Azure.Core;
using Azure.Identity;
using AzureKeyVaultDesktop.Core.Models;
using AzureKeyVaultDesktop.Core.Services.Settings;

namespace AzureKeyVaultDesktop.Core.Services.Auth;

public class AzureIdentityAuthService : IAuthService
{
    private const string ArmDefaultScope = "https://management.azure.com/.default";
    private const string AuthRecordFileName = "authrecord.json";

    private readonly ISettingsService _settingsService;
    private InteractiveBrowserCredential? _credential;

    public AzureIdentityAuthService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public bool IsSignedIn => CurrentAccount is not null;

    public AccountInfo? CurrentAccount { get; private set; }

    public TokenCredential Credential => _credential
        ?? throw new InvalidOperationException(
            $"{nameof(Credential)} is only available after {nameof(TryRestoreSessionAsync)} or {nameof(SignInInteractiveAsync)} has completed successfully.");

    public async Task<bool> TryRestoreSessionAsync(CancellationToken ct = default)
    {
        var record = await LoadAuthenticationRecordAsync(ct);
        if (record is null)
        {
            return false;
        }

        var credential = BuildCredential(record);

        // MSAL attempts a silent (cache/refresh-token) token acquisition first when an
        // AuthenticationRecord is supplied, and only falls back to an interactive browser
        // prompt if that fails. A bounded timeout here lets a genuinely silent restore
        // (normally well under a second) succeed, while preventing an unexpected browser
        // popup during this background "restore session on startup" check. If the cache is
        // no longer valid, we bail out and let the user trigger SignInInteractiveAsync
        // explicitly from the Login screen instead.
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

        try
        {
            await credential.GetTokenAsync(new TokenRequestContext([ArmDefaultScope]), linkedCts.Token);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (AuthenticationFailedException)
        {
            return false;
        }

        _credential = credential;
        CurrentAccount = ToAccountInfo(record);
        return true;
    }

    public async Task<AccountInfo> SignInInteractiveAsync(CancellationToken ct = default)
    {
        var credential = BuildCredential(authenticationRecord: null);
        var record = await credential.AuthenticateAsync(ct);

        _credential = credential;
        CurrentAccount = ToAccountInfo(record);

        await SaveAuthenticationRecordAsync(record, ct);
        return CurrentAccount;
    }

    public Task SignOutAsync()
    {
        _credential = null;
        CurrentAccount = null;

        var path = Path.Combine(_settingsService.SettingsFolder, AuthRecordFileName);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    private InteractiveBrowserCredential BuildCredential(AuthenticationRecord? authenticationRecord)
    {
        var settings = _settingsService.Current;
        if (string.IsNullOrWhiteSpace(settings.ClientId))
        {
            throw new InvalidOperationException("A Client ID must be configured in Settings before signing in.");
        }

        var options = new InteractiveBrowserCredentialOptions
        {
            ClientId = settings.ClientId,
            TenantId = string.IsNullOrWhiteSpace(settings.TenantId) ? "organizations" : settings.TenantId,
            RedirectUri = new Uri("http://localhost"),
            TokenCachePersistenceOptions = new TokenCachePersistenceOptions
            {
                Name = "AzureKeyVaultDesktop.msalcache",
                UnsafeAllowUnencryptedStorage = true,
            },
            AuthenticationRecord = authenticationRecord,
        };

        // A Key Vault the user has (e.g. guest/B2B) access to can live in a different tenant
        // than the one they signed in against. Azure.Identity refuses cross-tenant token
        // requests by default as a safety guard — this app's whole point is browsing whatever
        // vaults the signed-in identity can reach, across tenants, so opt in to all of them.
        options.AdditionallyAllowedTenants.Add("*");

        return new InteractiveBrowserCredential(options);
    }

    private static AccountInfo ToAccountInfo(AuthenticationRecord record) =>
        new(record.Username, record.Username, record.TenantId);

    private async Task<AuthenticationRecord?> LoadAuthenticationRecordAsync(CancellationToken ct)
    {
        var path = Path.Combine(_settingsService.SettingsFolder, AuthRecordFileName);
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            await using var stream = File.OpenRead(path);
            return await AuthenticationRecord.DeserializeAsync(stream, ct);
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    private async Task SaveAuthenticationRecordAsync(AuthenticationRecord record, CancellationToken ct)
    {
        Directory.CreateDirectory(_settingsService.SettingsFolder);
        var path = Path.Combine(_settingsService.SettingsFolder, AuthRecordFileName);
        await using var stream = File.Create(path);
        await record.SerializeAsync(stream, ct);
    }
}
