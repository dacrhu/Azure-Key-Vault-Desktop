using System.Text.RegularExpressions;

namespace AzureKeyVaultDesktop.Core.Services.KeyVault;

/// <summary>Shared secret-name validation, used both by the create-secret service call and
/// by the UI for immediate client-side feedback before it even attempts the call.</summary>
public static partial class SecretNameValidator
{
    public static bool IsValid(string name) => !string.IsNullOrEmpty(name) && ValidPattern().IsMatch(name);

    [GeneratedRegex("^[0-9a-zA-Z-]+$")]
    private static partial Regex ValidPattern();
}
