using AzureKeyVaultDesktop.Core.Models;

namespace AzureKeyVaultDesktop.Core.Services.KeyVault;

public interface IKeyVaultManagementService
{
    /// <summary>Enumerates Key Vaults across every subscription the signed-in user can see,
    /// filtered down to ones they actually have secrets data-plane access to — ARM's vault
    /// listing only requires subscription-level Reader access and says nothing about RBAC or
    /// access-policy grants on the vault itself, so each candidate is probed individually. A
    /// subscription that fails to enumerate (e.g. no Reader access) is skipped and reported
    /// via <paramref name="onSubscriptionWarning"/> rather than aborting the whole listing.
    /// Results are cached for the current signed-in identity; pass
    /// <paramref name="forceRefresh"/> to bypass the cache (e.g. an explicit "Refresh").</summary>
    IAsyncEnumerable<VaultSummary> ListAccessibleVaultsAsync(
        bool forceRefresh = false,
        Action<string, string>? onSubscriptionWarning = null,
        CancellationToken ct = default);
}
