namespace AzureKeyVaultDesktop.Core.Models;

public class AppSettings
{
    public string? ClientId { get; set; }

    /// <summary>Null means the "organizations" multi-tenant authority.</summary>
    public string? TenantId { get; set; }

    public int ClipboardAutoClearSeconds { get; set; } = 30;

    /// <summary>UI language code: "en", "hu", or "de".</summary>
    public string Language { get; set; } = "en";
}
