namespace AzureKeyVaultDesktop.Core.Services.Clipboard;

public interface IClipboardService
{
    /// <summary>Copies a secret value to the clipboard, then clears it again after
    /// <paramref name="autoClearAfter"/> — but only if the clipboard still contains exactly
    /// what this call put there, so a clear never clobbers something the user copied since.</summary>
    Task CopySecretAsync(string value, TimeSpan autoClearAfter, CancellationToken ct = default);

    /// <summary>Fires once per second while a clear is pending, carrying the seconds remaining.</summary>
    event EventHandler<int>? CountdownTick;

    /// <summary>Best-effort synchronous clear for use during app shutdown.</summary>
    void ClearPendingNow();
}
