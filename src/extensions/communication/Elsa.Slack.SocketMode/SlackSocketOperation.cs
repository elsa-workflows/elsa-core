namespace Elsa.Slack.SocketMode;

/// <summary>Owns one exposed cancellation token, its first callback completion and a private deadline.</summary>
/// <remarks>The caller closes the body before disposing its service scope, then disposes this owner.</remarks>
internal sealed class SlackSocketOperation(Action cancellationFailed, Action<SlackSocketOperation> settled) : IAsyncDisposable
{
    private readonly object _gate = new();
    private readonly CancellationTokenSource _exposed = new();
    private readonly CancellationTokenSource _watcherStop = new();
    private Task _watcher = Task.CompletedTask;
    private Task? _cancellation;
    private bool _bodyClosed;

    internal CancellationToken Token => _exposed.Token;

    // Start outside the owner's gate, including an explicitly configured sub-millisecond deadline.
    internal void StartDeadline(TimeSpan timeout, Action expired) => _watcher = WatchAsync(timeout, expired);

    private async Task WatchAsync(TimeSpan timeout, Action expired)
    {
        try
        {
            // Provider callbacks cannot delay this independent deadline.
            await Task.Delay(timeout, _watcherStop.Token);
            expired();
        }
        catch (OperationCanceledException) when (_watcherStop.IsCancellationRequested)
        {
        }
    }

    internal Task RequestCancellation()
    {
        lock (_gate)
        {
            // Subsequent CancelAsync calls may return before the FIRST callbacks have finished.
            return _bodyClosed ? _cancellation ?? Task.CompletedTask : _cancellation ??= _exposed.CancelAsync();
        }
    }

    internal async Task CloseBodyAsync()
    {
        Task callbacks;
        lock (_gate)
        {
            _bodyClosed = true;
            callbacks = _cancellation ?? Task.CompletedTask;
        }
        try
        {
            await callbacks;
        }
        catch (Exception)
        {
            cancellationFailed();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await CloseBodyAsync();
        await _watcherStop.CancelAsync();
        await _watcher;
        settled(this);
        _exposed.Dispose();
        _watcherStop.Dispose();
    }
}
