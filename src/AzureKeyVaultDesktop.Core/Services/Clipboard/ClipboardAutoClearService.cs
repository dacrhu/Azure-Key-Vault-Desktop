namespace AzureKeyVaultDesktop.Core.Services.Clipboard;

public class ClipboardAutoClearService : IClipboardService, IDisposable
{
    private readonly IClipboardTextAccessor _accessor;
    private readonly object _gate = new();
    private CancellationTokenSource? _pendingClearCts;
    private string? _lastSetValue;

    public ClipboardAutoClearService(IClipboardTextAccessor accessor)
    {
        _accessor = accessor;
    }

    public event EventHandler<int>? CountdownTick;

    public async Task CopySecretAsync(string value, TimeSpan autoClearAfter, CancellationToken ct = default)
    {
        CancellationTokenSource cts;
        lock (_gate)
        {
            // A new copy supersedes any pending clear — cancel it so it doesn't wipe
            // out the value we're about to set.
            _pendingClearCts?.Cancel();
            _pendingClearCts?.Dispose();
            cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _pendingClearCts = cts;
            _lastSetValue = value;
        }

        await _accessor.SetTextAsync(value);

        _ = RunCountdownAndClearAsync(value, autoClearAfter, cts);
    }

    private async Task RunCountdownAndClearAsync(string value, TimeSpan autoClearAfter, CancellationTokenSource cts)
    {
        try
        {
            var remaining = (int)Math.Ceiling(autoClearAfter.TotalSeconds);
            while (remaining > 0)
            {
                CountdownTick?.Invoke(this, remaining);
                await Task.Delay(TimeSpan.FromSeconds(1), cts.Token);
                remaining--;
            }

            CountdownTick?.Invoke(this, 0);
            await ClearIfUnchangedAsync(value);
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer copy — leave the clipboard alone, the new copy owns it now.
        }
        finally
        {
            lock (_gate)
            {
                if (ReferenceEquals(_pendingClearCts, cts))
                {
                    _pendingClearCts = null;
                }
            }

            cts.Dispose();
        }
    }

    private async Task ClearIfUnchangedAsync(string expectedValue)
    {
        // Only clear if the clipboard still holds exactly what we put there — if the user
        // copied something else in the meantime, that's theirs now, leave it alone.
        var current = await _accessor.GetTextAsync();
        if (current == expectedValue)
        {
            await _accessor.SetTextAsync(string.Empty);
        }

        lock (_gate)
        {
            if (_lastSetValue == expectedValue)
            {
                _lastSetValue = null;
            }
        }
    }

    public void ClearPendingNow()
    {
        CancellationTokenSource? cts;
        string? expected;
        lock (_gate)
        {
            cts = _pendingClearCts;
            expected = _lastSetValue;
        }

        if (cts is null || expected is null)
        {
            return;
        }

        cts.Cancel();

        // Best-effort on shutdown: block briefly rather than leaving a secret on the clipboard.
        var current = _accessor.GetTextAsync().GetAwaiter().GetResult();
        if (current == expected)
        {
            _accessor.SetTextAsync(string.Empty).GetAwaiter().GetResult();
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _pendingClearCts?.Cancel();
            _pendingClearCts?.Dispose();
            _pendingClearCts = null;
        }
    }
}
