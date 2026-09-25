using AzureKeyVaultDesktop.Core.Services.Updates;
using Xunit;

namespace AzureKeyVaultDesktop.Core.Tests;

public class ReleaseVersionComparerTests
{
    [Theory]
    [InlineData("1.2.0", "1.1.9")]
    [InlineData("v1.2.0", "1.1.9")]
    [InlineData("2.0.0", "1.9.9")]
    [InlineData("1.0.10", "1.0.9")]
    public void NewerTag_ReturnsTrue(string latestTag, string currentVersion)
    {
        Assert.True(ReleaseVersionComparer.IsNewer(latestTag, currentVersion));
    }

    [Theory]
    [InlineData("1.0.0", "1.0.0")]
    [InlineData("1.0.0", "1.0.1")]
    [InlineData("1.0.0", "2.0.0")]
    public void SameOrOlderTag_ReturnsFalse(string latestTag, string currentVersion)
    {
        Assert.False(ReleaseVersionComparer.IsNewer(latestTag, currentVersion));
    }

    [Theory]
    [InlineData("not-a-version", "1.0.0")]
    [InlineData("1.0.0", "not-a-version")]
    public void UnparsableVersion_ReturnsFalse(string latestTag, string currentVersion)
    {
        Assert.False(ReleaseVersionComparer.IsNewer(latestTag, currentVersion));
    }
}
