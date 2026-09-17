using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using AzureKeyVaultDesktop.Core.Services.Clipboard;

namespace AzureKeyVaultDesktop.App.Services;

public class AvaloniaClipboardTextAccessor : IClipboardTextAccessor
{
    public async Task SetTextAsync(string text)
    {
        var clipboard = GetClipboard();
        if (clipboard is not null)
        {
            await clipboard.SetTextAsync(text);
        }
    }

    public async Task<string?> GetTextAsync()
    {
        var clipboard = GetClipboard();
        return clipboard is null ? null : await clipboard.GetTextAsync();
    }

    private static Avalonia.Input.Platform.IClipboard? GetClipboard()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            && desktop.MainWindow is Window mainWindow)
        {
            return TopLevel.GetTopLevel(mainWindow)?.Clipboard;
        }

        return null;
    }
}
