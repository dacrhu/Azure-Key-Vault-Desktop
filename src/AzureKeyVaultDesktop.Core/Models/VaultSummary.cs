namespace AzureKeyVaultDesktop.Core.Models;

public record VaultSummary(
    string Name,
    Uri VaultUri,
    string SubscriptionId,
    string SubscriptionDisplayName,
    string ResourceGroup,
    string Location);
