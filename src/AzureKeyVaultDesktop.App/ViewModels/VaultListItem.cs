using AzureKeyVaultDesktop.Core.Models;

namespace AzureKeyVaultDesktop.App.ViewModels;

/// <summary>One row in the (filtered, grouped, sorted) vault list. HasGroupHeader is true only
/// for the first vault of each subscription group, so the view can render a header above it.</summary>
public sealed class VaultListItem
{
    public VaultListItem(VaultSummary vault, bool hasGroupHeader, string groupHeader)
    {
        Vault = vault;
        HasGroupHeader = hasGroupHeader;
        GroupHeader = groupHeader;
    }

    public VaultSummary Vault { get; }

    public bool HasGroupHeader { get; }

    public string GroupHeader { get; }
}
