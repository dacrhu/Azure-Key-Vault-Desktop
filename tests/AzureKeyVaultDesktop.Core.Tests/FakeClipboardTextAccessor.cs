using AzureKeyVaultDesktop.Core.Services.Clipboard;

namespace AzureKeyVaultDesktop.Core.Tests;

internal class FakeClipboardTextAccessor : IClipboardTextAccessor
{
    public string? Text { get; private set; }

    public Task SetTextAsync(string text)
    {
        Text = text;
        return Task.CompletedTask;
    }

    public Task<string?> GetTextAsync() => Task.FromResult(Text);
}
