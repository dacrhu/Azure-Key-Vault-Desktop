using AzureKeyVaultDesktop.Core.Models;

namespace AzureKeyVaultDesktop.Core.Services.Updates;

/// <summary>Checks GitHub Releases for a newer tagged version than the one currently running.
/// Never throws — a failed/offline check should be silent, not an error shown to the user.</summary>
public interface IUpdateCheckService
{
    /// <summary>Returns update info if a newer release is available, otherwise null. The result
    /// is cached for the lifetime of the service so repeated calls (e.g. re-navigating to the
    /// vault list) don't re-hit the GitHub API.</summary>
    Task<UpdateCheckResult?> CheckForUpdateAsync(string currentVersion, CancellationToken ct = default);
}
