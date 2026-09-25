using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.KeyVault;
using Azure.ResourceManager.Resources;
using Azure.Security.KeyVault.Secrets;
using AzureKeyVaultDesktop.Core.Models;
using AzureKeyVaultDesktop.Core.Services.Auth;

namespace AzureKeyVaultDesktop.Core.Services.KeyVault;

public class KeyVaultManagementService : IKeyVaultManagementService
{
    private const int MaxConcurrentSubscriptions = 8;
    private const int MaxConcurrentVaultProbes = 8;

    private readonly IAuthService _authService;
    private readonly IVaultListCacheStore _cacheStore;
    private TokenCredential? _lastCredential;
    private ArmClient? _armClient;
    private List<VaultSummary>? _cachedVaults;
    private bool _needsStartupReconcile = true;

    public KeyVaultManagementService(IAuthService authService, IVaultListCacheStore cacheStore)
    {
        _authService = authService;
        _cacheStore = cacheStore;
    }

    public bool NeedsStartupReconcile => _needsStartupReconcile;

    public async IAsyncEnumerable<VaultSummary> ListAccessibleVaultsAsync(
        bool forceRefresh = false,
        Action<string, string>? onSubscriptionWarning = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var (armClient, credential) = GetArmClient();
        var accountKey = GetAccountKey();

        if (!forceRefresh)
        {
            // Nothing in memory yet this process — fall back to whatever was persisted from
            // the previous run instead of blocking on a fresh, multi-subscription,
            // per-vault-probed fetch just to paint the first frame.
            if (_cachedVaults is null && accountKey is not null)
            {
                _cachedVaults = await _cacheStore.LoadAsync(accountKey, ct);
            }

            if (_cachedVaults is not null)
            {
                foreach (var vault in _cachedVaults)
                {
                    ct.ThrowIfCancellationRequested();
                    yield return vault;
                }

                yield break;
            }
        }

        var subscriptions = new List<SubscriptionResource>();
        await foreach (var subscription in armClient.GetSubscriptions().GetAllAsync(cancellationToken: ct))
        {
            subscriptions.Add(subscription);
        }

        // Unbounded channel so each subscription's vaults reach the caller as soon as they're
        // confirmed, instead of buffering everything until the slowest subscription finishes.
        var channel = Channel.CreateUnbounded<VaultSummary>();
        using var subscriptionThrottle = new SemaphoreSlim(MaxConcurrentSubscriptions);
        using var probeThrottle = new SemaphoreSlim(MaxConcurrentVaultProbes);

        var producers = subscriptions.Select(async subscription =>
        {
            await subscriptionThrottle.WaitAsync(ct);
            try
            {
                var candidates = new List<VaultSummary>();
                await foreach (var vault in subscription.GetKeyVaultsAsync(cancellationToken: ct))
                {
                    var vaultUri = vault.Data.Properties?.VaultUri;
                    if (vaultUri is null)
                    {
                        continue;
                    }

                    candidates.Add(new VaultSummary(
                        vault.Data.Name,
                        vaultUri,
                        subscription.Data.SubscriptionId,
                        subscription.Data.DisplayName,
                        vault.Data.Id.ResourceGroupName ?? string.Empty,
                        vault.Data.Location.ToString()));
                }

                // ARM's vault listing only checks subscription-level Reader access — it says
                // nothing about whether the signed-in user can actually read secrets inside a
                // given vault (that's a separate data-plane RBAC role or access policy). Probe
                // each candidate and only surface vaults that genuinely grant access, so this
                // reflects "vaults you have rights to", not "vaults that exist in a
                // subscription you can merely see".
                await Task.WhenAll(candidates.Select(async candidate =>
                {
                    await probeThrottle.WaitAsync(ct);
                    try
                    {
                        if (await HasSecretsAccessAsync(candidate.VaultUri, credential, ct))
                        {
                            await channel.Writer.WriteAsync(candidate, ct);
                        }
                    }
                    finally
                    {
                        probeThrottle.Release();
                    }
                }));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                onSubscriptionWarning?.Invoke(subscription.Data.DisplayName, ex.Message);
            }
            finally
            {
                subscriptionThrottle.Release();
            }
        }).ToList();

        _ = Task.WhenAll(producers).ContinueWith(
            _ => channel.Writer.TryComplete(),
            CancellationToken.None,
            TaskContinuationOptions.None,
            TaskScheduler.Default);

        var collected = new List<VaultSummary>();
        await foreach (var vault in channel.Reader.ReadAllAsync(ct))
        {
            collected.Add(vault);
            yield return vault;
        }

        _cachedVaults = collected;
        _needsStartupReconcile = false;

        if (accountKey is not null)
        {
            await _cacheStore.SaveAsync(accountKey, collected, ct);
        }
    }

    private static async Task<bool> HasSecretsAccessAsync(Uri vaultUri, TokenCredential credential, CancellationToken ct)
    {
        try
        {
            var client = new SecretClient(vaultUri, credential);
            await foreach (var _ in client.GetPropertiesOfSecretsAsync(ct))
            {
                return true;
            }

            return true; // empty vault, but the call itself succeeded, so access is real
        }
        catch (RequestFailedException)
        {
            // Most commonly 403 Forbidden — no RBAC role or access policy grants secrets
            // access here. Any API-level failure means access can't be confirmed, so the
            // vault is excluded rather than shown speculatively.
            return false;
        }
    }

    private (ArmClient, TokenCredential) GetArmClient()
    {
        var credential = _authService.Credential;
        if (_armClient is null || !ReferenceEquals(_lastCredential, credential))
        {
            // A different identity signed in since this was built — a stale ArmClient (and any
            // vault list cached under the previous identity) must not leak into this session.
            _armClient = new ArmClient(credential);
            _cachedVaults = null;
            _needsStartupReconcile = true;
            _lastCredential = credential;
        }

        return (_armClient, credential);
    }

    private string? GetAccountKey()
    {
        var account = _authService.CurrentAccount;
        return account is null ? null : $"{account.Username}|{account.TenantId}";
    }
}
