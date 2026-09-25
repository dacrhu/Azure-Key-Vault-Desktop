namespace AzureKeyVaultDesktop.Core.Services.Updates;

/// <summary>Pure semver comparison for release tags (plain semver, no "v" prefix — see
/// release.yml) against the running app's version.</summary>
public static class ReleaseVersionComparer
{
    public static bool IsNewer(string latestTag, string currentVersion)
    {
        if (!Version.TryParse(Normalize(latestTag), out var latest))
        {
            return false;
        }

        if (!Version.TryParse(Normalize(currentVersion), out var current))
        {
            return false;
        }

        return latest > current;
    }

    private static string Normalize(string version) => version.Trim().TrimStart('v', 'V');
}
