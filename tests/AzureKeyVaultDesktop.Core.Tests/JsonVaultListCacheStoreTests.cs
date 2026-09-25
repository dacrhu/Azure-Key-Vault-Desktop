using AzureKeyVaultDesktop.Core.Models;
using AzureKeyVaultDesktop.Core.Services.KeyVault;
using AzureKeyVaultDesktop.Core.Services.Settings;
using Xunit;

namespace AzureKeyVaultDesktop.Core.Tests;

public class JsonVaultListCacheStoreTests : IDisposable
{
    private readonly string _tempDir;
    private readonly JsonVaultListCacheStore _store;

    public JsonVaultListCacheStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "AzureKeyVaultDesktopTests_" + Guid.NewGuid());
        _store = new JsonVaultListCacheStore(new JsonSettingsService(_tempDir));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    private static readonly VaultSummary SampleVault = new(
        "my-vault",
        new Uri("https://my-vault.vault.azure.net/"),
        "sub-id",
        "My Subscription",
        "my-rg",
        "westeurope");

    [Fact]
    public async Task SaveAndLoad_RoundTripsVaults()
    {
        await _store.SaveAsync("user@contoso.com|tenant-id", [SampleVault]);

        var reloaded = await _store.LoadAsync("user@contoso.com|tenant-id");

        var vault = Assert.Single(reloaded!);
        Assert.Equal(SampleVault, vault);
    }

    [Fact]
    public async Task LoadAsync_WithDifferentAccountKey_ReturnsNull()
    {
        await _store.SaveAsync("user@contoso.com|tenant-id", [SampleVault]);

        var reloaded = await _store.LoadAsync("someone-else@contoso.com|tenant-id");

        Assert.Null(reloaded);
    }

    [Fact]
    public async Task LoadAsync_WithNoExistingFile_ReturnsNull()
    {
        var reloaded = await _store.LoadAsync("user@contoso.com|tenant-id");

        Assert.Null(reloaded);
    }
}
