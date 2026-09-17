namespace AzureKeyVaultDesktop.Core.Services.Clipboard;

/// <summary>Thin primitive over the OS clipboard. The Avalonia app provides the real
/// implementation via TopLevel.Clipboard; Core only depends on this interface so the
/// auto-clear timer/guard logic below can be unit-tested with a fake.</summary>
public interface IClipboardTextAccessor
{
    Task SetTextAsync(string text);

    Task<string?> GetTextAsync();
}
