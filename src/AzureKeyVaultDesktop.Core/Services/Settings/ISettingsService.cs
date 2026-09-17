using AzureKeyVaultDesktop.Core.Models;

namespace AzureKeyVaultDesktop.Core.Services.Settings;

public interface ISettingsService
{
    AppSettings Current { get; }

    /// <summary>Directory where settings.json (and the auth record) live, per OS conventions.</summary>
    string SettingsFolder { get; }

    Task LoadAsync(CancellationToken ct = default);

    Task SaveAsync(CancellationToken ct = default);
}
