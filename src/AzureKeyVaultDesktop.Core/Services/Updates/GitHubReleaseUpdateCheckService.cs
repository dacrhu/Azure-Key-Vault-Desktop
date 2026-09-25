using System.Net.Http.Headers;
using System.Text.Json;
using AzureKeyVaultDesktop.Core.Models;

namespace AzureKeyVaultDesktop.Core.Services.Updates;

public class GitHubReleaseUpdateCheckService : IUpdateCheckService, IDisposable
{
    private const string LatestReleaseApiUrl =
        "https://api.github.com/repos/dacrhu/Azure-Key-Vault-Desktop/releases/latest";

    private readonly HttpClient _httpClient;
    private readonly object _cacheLock = new();
    private Task<UpdateCheckResult?>? _cachedCheck;

    public GitHubReleaseUpdateCheckService()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        // The GitHub API rejects requests with no User-Agent.
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("AzureKeyVaultDesktop", "1.0"));
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
    }

    public Task<UpdateCheckResult?> CheckForUpdateAsync(string currentVersion, CancellationToken ct = default)
    {
        lock (_cacheLock)
        {
            return _cachedCheck ??= CheckForUpdateCoreAsync(currentVersion, ct);
        }
    }

    private async Task<UpdateCheckResult?> CheckForUpdateCoreAsync(string currentVersion, CancellationToken ct)
    {
        try
        {
            using var response = await _httpClient.GetAsync(LatestReleaseApiUrl, ct);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            using var document = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

            var tagName = document.RootElement.TryGetProperty("tag_name", out var tagProp) ? tagProp.GetString() : null;
            var releaseUrl = document.RootElement.TryGetProperty("html_url", out var urlProp) ? urlProp.GetString() : null;

            if (string.IsNullOrWhiteSpace(tagName) || string.IsNullOrWhiteSpace(releaseUrl))
            {
                return null;
            }

            return ReleaseVersionComparer.IsNewer(tagName, currentVersion)
                ? new UpdateCheckResult(tagName, releaseUrl)
                : null;
        }
        catch (Exception) when (ct.IsCancellationRequested is false)
        {
            // Best-effort only — offline, rate-limited, or malformed response should never
            // surface as an error to the user over a version check.
            return null;
        }
    }

    public void Dispose() => _httpClient.Dispose();
}
