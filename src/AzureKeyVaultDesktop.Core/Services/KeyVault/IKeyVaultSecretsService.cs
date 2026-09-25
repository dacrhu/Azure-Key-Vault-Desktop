using AzureKeyVaultDesktop.Core.Models;

namespace AzureKeyVaultDesktop.Core.Services.KeyVault;

/// <summary>Secrets-only access to a Key Vault's data plane. Deliberately has no operations
/// for certificates or keys — the app must never read or manage those.</summary>
public interface IKeyVaultSecretsService
{
    /// <summary>Lists secret names/metadata for a vault. Results are cached per vault for the
    /// current signed-in identity; pass <paramref name="forceRefresh"/> to bypass the cache
    /// (e.g. an explicit user "Refresh" action).</summary>
    IAsyncEnumerable<SecretSummary> ListSecretsAsync(Uri vaultUri, bool forceRefresh = false, CancellationToken ct = default);

    /// <summary>Lists every version of one secret, newest information first is not guaranteed —
    /// callers should sort as needed. Exactly one entry has <c>IsCurrent == true</c> (the
    /// version <see cref="GetSecretValueAsync"/> returns when no version is specified).</summary>
    IAsyncEnumerable<SecretVersionSummary> ListSecretVersionsAsync(Uri vaultUri, string name, CancellationToken ct = default);

    /// <summary>Fetches a secret's value (a specific version, or the current one when
    /// <paramref name="version"/> is null). Call only in direct response to an explicit user
    /// "reveal"/"copy" action — never to bulk-populate a list.</summary>
    Task<string> GetSecretValueAsync(Uri vaultUri, string name, string? version = null, CancellationToken ct = default);

    Task CreateSecretAsync(Uri vaultUri, string name, string value, string? contentType = null, CancellationToken ct = default);

    /// <summary>Adds a new version to an existing secret (Key Vault never overwrites a version
    /// in place — this is exactly what the Azure Portal's "New Version" does).</summary>
    Task UpdateSecretValueAsync(Uri vaultUri, string name, string value, string? contentType = null, CancellationToken ct = default);
}
