using Azure.Core;
using AzureKeyVaultDesktop.Core.Models;

namespace AzureKeyVaultDesktop.Core.Services.Auth;

public interface IAuthService
{
    bool IsSignedIn { get; }

    AccountInfo? CurrentAccount { get; }

    /// <summary>Shared credential handed to ArmClient/SecretClient. Triggers interactive sign-in
    /// on first use only if no cached/silent session is available.</summary>
    TokenCredential Credential { get; }

    /// <summary>Attempts a silent sign-in from a previously persisted session. Returns false
    /// (without prompting) if no valid cached session exists.</summary>
    Task<bool> TryRestoreSessionAsync(CancellationToken ct = default);

    /// <summary>Opens the system browser for interactive sign-in.</summary>
    Task<AccountInfo> SignInInteractiveAsync(CancellationToken ct = default);

    Task SignOutAsync();
}
