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
    /// <paramref name="forceRefresh"/> to bypass the cache (e.g. an explicit "Refresh"). The
    /// very first (non-force) call in a fresh process falls back to the on-disk cache from the
    /// previous run when nothing is in memory yet, so callers get an instant, non-empty result
    /// instead of blocking on a live fetch — see <see cref="NeedsStartupReconcile"/>.</summary>
    IAsyncEnumerable<VaultSummary> ListAccessibleVaultsAsync(
        bool forceRefresh = false,
        Action<string, string>? onSubscriptionWarning = null,
        CancellationToken ct = default);

    /// <summary>True until the first live Azure fetch completes in this process (whether
    /// triggered by an explicit refresh, or because a non-force call had no cache — on-disk or
    /// in-memory — to fall back to). A caller that just painted an on-disk-cached list can check
    /// this right after to decide whether a background reconcile pass against Azure is still
    /// needed to catch vaults added/removed since that cache was written — or whether the load
    /// it just did already was that live fetch, making a second one redundant.</summary>
    bool NeedsStartupReconcile { get; }
}
