using System.Runtime.InteropServices;
using System.Text.Json;
using AzureKeyVaultDesktop.Core.Models;

namespace AzureKeyVaultDesktop.Core.Services.Settings;

public class JsonSettingsService : ISettingsService
{
    private const string AppFolderName = "AzureKeyVaultDesktop";
    private const string SettingsFileName = "settings.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    public AppSettings Current { get; private set; } = new();

    public string SettingsFolder { get; }

    public JsonSettingsService()
        : this(ResolveSettingsFolder())
    {
    }

    /// <summary>Allows tests to point at an isolated temp folder instead of the real per-OS location.</summary>
    internal JsonSettingsService(string settingsFolder)
    {
        SettingsFolder = settingsFolder;
    }

    public async Task LoadAsync(CancellationToken ct = default)
    {
        var path = Path.Combine(SettingsFolder, SettingsFileName);
        if (!File.Exists(path))
        {
            Current = new AppSettings();
            return;
        }

        await using var stream = File.OpenRead(path);
        Current = await JsonSerializer.DeserializeAsync<AppSettings>(stream, cancellationToken: ct)
            ?? new AppSettings();
    }

    public async Task SaveAsync(CancellationToken ct = default)
    {
        Directory.CreateDirectory(SettingsFolder);
        var path = Path.Combine(SettingsFolder, SettingsFileName);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, Current, SerializerOptions, ct);
    }

    internal static string ResolveSettingsFolder()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, AppFolderName);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, "Library", "Application Support", AppFolderName);
        }

        // Linux and other Unix-likes: XDG Base Directory spec.
        var xdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        if (!string.IsNullOrEmpty(xdgConfigHome))
        {
            return Path.Combine(xdgConfigHome, AppFolderName);
        }

        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(homeDir, ".config", AppFolderName);
    }
}
