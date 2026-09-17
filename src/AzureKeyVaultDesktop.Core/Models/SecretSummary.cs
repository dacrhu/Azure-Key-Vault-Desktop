namespace AzureKeyVaultDesktop.Core.Models;

public record SecretSummary(string Name, bool Enabled, DateTimeOffset? UpdatedOn);
