using AzureKeyVaultDesktop.Core.Models;

namespace AzureKeyVaultDesktop.Core.Services.KeyVault;

/// <summary>Persists the last-known accessible-vaults list to disk (names, URIs, and
/// subscription metadata only — never secret values) so the next app launch can paint the
/// vault list instantly from it while <see cref="KeyVaultManagementService"/> reconciles
/// against Azure in the background, instead of the UI sitting empty for the duration of a
/// fresh (multi-subscription, per-vault-probed) fetch.</summary>
public interface IVaultListCacheStore
{
    /// <summary>Returns the cached vaults if a cache file exists and was written for this exact
    /// <paramref name="accountKey"/>; null otherwise (no cache yet, an unreadable file, or a
    /// cache written under a different signed-in identity — never served across identities).</summary>
    Task<List<VaultSummary>?> LoadAsync(string accountKey, CancellationToken ct = default);

    Task SaveAsync(string accountKey, IReadOnlyList<VaultSummary> vaults, CancellationToken ct = default);
}
