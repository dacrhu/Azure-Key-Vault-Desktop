using AzureKeyVaultDesktop.Core.Services.KeyVault;
using Xunit;

namespace AzureKeyVaultDesktop.Core.Tests;

public class SecretNameValidationTests
{
    [Theory]
    [InlineData("my-secret")]
    [InlineData("MySecret123")]
    [InlineData("a")]
    public void ValidNames_AreAccepted(string name)
    {
        Assert.True(SecretNameValidator.IsValid(name));
    }

    [Theory]
    [InlineData("")]
    [InlineData("my_secret")]
    [InlineData("my secret")]
    [InlineData("my.secret")]
    [InlineData("secret!")]
    public void InvalidNames_AreRejected(string name)
    {
        Assert.False(SecretNameValidator.IsValid(name));
    }
}
