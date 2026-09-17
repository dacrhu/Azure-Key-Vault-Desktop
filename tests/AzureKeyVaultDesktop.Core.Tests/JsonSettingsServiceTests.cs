using AzureKeyVaultDesktop.Core.Services.Settings;
using Xunit;

namespace AzureKeyVaultDesktop.Core.Tests;

public class JsonSettingsServiceTests : IDisposable
{
    private readonly string _tempDir;

    public JsonSettingsServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "AzureKeyVaultDesktopTests_" + Guid.NewGuid());
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAndLoad_RoundTripsSettings()
    {
        var service = new JsonSettingsService(_tempDir);
        service.Current.ClientId = "11111111-1111-1111-1111-111111111111";
        service.Current.TenantId = "22222222-2222-2222-2222-222222222222";
        service.Current.ClipboardAutoClearSeconds = 45;

        await service.SaveAsync();

        var reloaded = new JsonSettingsService(_tempDir);
        await reloaded.LoadAsync();

        Assert.Equal(service.Current.ClientId, reloaded.Current.ClientId);
        Assert.Equal(service.Current.TenantId, reloaded.Current.TenantId);
        Assert.Equal(service.Current.ClipboardAutoClearSeconds, reloaded.Current.ClipboardAutoClearSeconds);
    }

    [Fact]
    public async Task LoadAsync_WithNoExistingFile_ReturnsDefaults()
    {
        var service = new JsonSettingsService(_tempDir);
        await service.LoadAsync();

        Assert.Null(service.Current.ClientId);
        Assert.Equal(30, service.Current.ClipboardAutoClearSeconds);
    }

    [Fact]
    public void ResolveSettingsFolder_EndsWithAppFolderName()
    {
        var folder = JsonSettingsService.ResolveSettingsFolder();
        Assert.EndsWith("AzureKeyVaultDesktop", folder);
    }
}
