using AzureKeyVaultDesktop.Core.Services.Clipboard;
using Xunit;

namespace AzureKeyVaultDesktop.Core.Tests;

public class ClipboardAutoClearServiceTests
{
    [Fact]
    public async Task CopySecretAsync_SetsClipboardImmediately()
    {
        var accessor = new FakeClipboardTextAccessor();
        var service = new ClipboardAutoClearService(accessor);

        await service.CopySecretAsync("s3cr3t", TimeSpan.FromSeconds(1));

        Assert.Equal("s3cr3t", accessor.Text);
    }

    [Fact]
    public async Task ClipboardIsCleared_AfterTimeoutElapses()
    {
        var accessor = new FakeClipboardTextAccessor();
        var service = new ClipboardAutoClearService(accessor);

        await service.CopySecretAsync("s3cr3t", TimeSpan.FromSeconds(1));
        await Task.Delay(TimeSpan.FromSeconds(2));

        Assert.Equal(string.Empty, accessor.Text);
    }

    [Fact]
    public async Task ClipboardIsNotCleared_IfUserCopiedSomethingElseMeanwhile()
    {
        var accessor = new FakeClipboardTextAccessor();
        var service = new ClipboardAutoClearService(accessor);

        await service.CopySecretAsync("s3cr3t", TimeSpan.FromSeconds(1));
        await accessor.SetTextAsync("something the user copied instead");
        await Task.Delay(TimeSpan.FromSeconds(2));

        Assert.Equal("something the user copied instead", accessor.Text);
    }

    [Fact]
    public async Task CopyingAgain_CancelsThePreviousPendingClear()
    {
        var accessor = new FakeClipboardTextAccessor();
        var service = new ClipboardAutoClearService(accessor);

        await service.CopySecretAsync("first", TimeSpan.FromSeconds(1));
        await Task.Delay(TimeSpan.FromMilliseconds(300));
        await service.CopySecretAsync("second", TimeSpan.FromSeconds(5));

        // The first clear would have fired by now if it hadn't been cancelled.
        await Task.Delay(TimeSpan.FromSeconds(1));

        Assert.Equal("second", accessor.Text);
    }

    [Fact]
    public async Task ClearPendingNow_ClearsImmediatelyIfClipboardStillHoldsWhatWeSet()
    {
        var accessor = new FakeClipboardTextAccessor();
        var service = new ClipboardAutoClearService(accessor);

        await service.CopySecretAsync("s3cr3t", TimeSpan.FromSeconds(30));
        service.ClearPendingNow();

        Assert.Equal(string.Empty, accessor.Text);
    }
}
