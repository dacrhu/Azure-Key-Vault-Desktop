namespace AzureKeyVaultDesktop.Core.Models;

public record SecretVersionSummary(string Version, bool Enabled, DateTimeOffset? UpdatedOn, bool IsCurrent, string? ContentType);
