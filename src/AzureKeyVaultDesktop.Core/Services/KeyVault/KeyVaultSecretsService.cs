using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Azure;
using Azure.Core;
using Azure.Security.KeyVault.Secrets;
using AzureKeyVaultDesktop.Core.Models;
using AzureKeyVaultDesktop.Core.Services.Auth;

namespace AzureKeyVaultDesktop.Core.Services.KeyVault;

/// <summary>Talks only to Azure.Security.KeyVault.Secrets — never reference
/// Azure.Security.KeyVault.Certificates or Azure.Security.KeyVault.Keys here.</summary>
public class KeyVaultSecretsService : IKeyVaultSecretsService
{
    private readonly IAuthService _authService;
    private readonly ConcurrentDictionary<Uri, SecretClient> _clients = new();
    private readonly ConcurrentDictionary<Uri, List<SecretSummary>> _listCache = new();
    private TokenCredential? _lastCredential;

    public KeyVaultSecretsService(IAuthService authService)
    {
        _authService = authService;
    }

    public async IAsyncEnumerable<SecretSummary> ListSecretsAsync(
        Uri vaultUri,
        bool forceRefresh = false,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var client = GetClient(vaultUri);

        if (!forceRefresh && _listCache.TryGetValue(vaultUri, out var cached))
        {
            foreach (var secret in cached)
            {
                ct.ThrowIfCancellationRequested();
                yield return secret;
            }

            yield break;
        }

        var collected = new List<SecretSummary>();
        await foreach (var properties in client.GetPropertiesOfSecretsAsync(ct))
        {
            var summary = new SecretSummary(properties.Name, properties.Enabled ?? true, properties.UpdatedOn);
            collected.Add(summary);
            yield return summary;
        }

        _listCache[vaultUri] = collected;
    }

    public async IAsyncEnumerable<SecretVersionSummary> ListSecretVersionsAsync(
        Uri vaultUri,
        string name,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var client = GetClient(vaultUri);

        string? currentVersion = null;
        try
        {
            var current = await client.GetSecretAsync(name, cancellationToken: ct);
            currentVersion = current.Value.Properties.Version;
        }
        catch (RequestFailedException)
        {
            // No accessible "current" version (e.g. the latest is disabled) — versions can
            // still be listed, just none will be marked current.
        }

        await foreach (var properties in client.GetPropertiesOfSecretVersionsAsync(name, ct))
        {
            yield return new SecretVersionSummary(
                properties.Version,
                properties.Enabled ?? true,
                properties.UpdatedOn,
                properties.Version == currentVersion);
        }
    }

    public async Task<string> GetSecretValueAsync(Uri vaultUri, string name, string? version = null, CancellationToken ct = default)
    {
        var client = GetClient(vaultUri);
        var response = await client.GetSecretAsync(name, version, ct);
        return response.Value.Value;
    }

    public async Task CreateSecretAsync(Uri vaultUri, string name, string value, CancellationToken ct = default)
    {
        if (!SecretNameValidator.IsValid(name))
        {
            throw new ArgumentException(
                "Secret names may only contain letters, digits, and hyphens.", nameof(name));
        }

        var client = GetClient(vaultUri);
        await client.SetSecretAsync(name, value, ct);
        _listCache.TryRemove(vaultUri, out _);
    }

    public async Task UpdateSecretValueAsync(Uri vaultUri, string name, string value, CancellationToken ct = default)
    {
        var client = GetClient(vaultUri);
        await client.SetSecretAsync(name, value, ct);
        _listCache.TryRemove(vaultUri, out _);
    }

    private SecretClient GetClient(Uri vaultUri)
    {
        var credential = _authService.Credential;
        if (!ReferenceEquals(_lastCredential, credential))
        {
            // A different identity signed in since these clients (and any cached lists) were
            // built — drop everything rather than risk serving one user's data plane access,
            // or another user's cached secret list, under a new session.
            _clients.Clear();
            _listCache.Clear();
            _lastCredential = credential;
        }

        return _clients.GetOrAdd(vaultUri, uri => new SecretClient(uri, credential));
    }
}
