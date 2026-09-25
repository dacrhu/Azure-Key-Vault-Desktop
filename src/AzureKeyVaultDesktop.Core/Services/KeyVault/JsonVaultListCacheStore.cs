using System.Text.Json;
using AzureKeyVaultDesktop.Core.Models;
using AzureKeyVaultDesktop.Core.Services.Settings;

namespace AzureKeyVaultDesktop.Core.Services.KeyVault;

public class JsonVaultListCacheStore : IVaultListCacheStore
{
    private const string CacheFileName = "vaultcache.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    private readonly ISettingsService _settingsService;

    public JsonVaultListCacheStore(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public async Task<List<VaultSummary>?> LoadAsync(string accountKey, CancellationToken ct = default)
    {
        var path = Path.Combine(_settingsService.SettingsFolder, CacheFileName);
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            await using var stream = File.OpenRead(path);
            var file = await JsonSerializer.DeserializeAsync<CacheFile>(stream, cancellationToken: ct);
            return file is not null && file.AccountKey == accountKey ? file.Vaults : null;
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

    public async Task SaveAsync(string accountKey, IReadOnlyList<VaultSummary> vaults, CancellationToken ct = default)
    {
        Directory.CreateDirectory(_settingsService.SettingsFolder);
        var path = Path.Combine(_settingsService.SettingsFolder, CacheFileName);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, new CacheFile(accountKey, vaults.ToList()), SerializerOptions, ct);
    }

    private record CacheFile(string AccountKey, List<VaultSummary> Vaults);
}
