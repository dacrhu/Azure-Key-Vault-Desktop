namespace AzureKeyVaultDesktop.Core.Models;

/// <summary>Returned only when the latest GitHub release is newer than the running app.</summary>
public record UpdateCheckResult(string LatestVersion, string ReleaseUrl);
