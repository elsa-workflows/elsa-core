using Microsoft.Extensions.Logging;

namespace Elsa.Common.RecurringTasks;

public class ScheduledTimer : IDisposable, IAsyncDisposable
{
    private readonly Func<Task> _action;
    private readonly Func<TimeSpan> _interval;
    private readonly Timer _timer;
    private readonly ILogger? _logger;

    public ScheduledTimer(Func<Task> action, Func<TimeSpan> interval, ILogger? logger = null)
    {
        _action = action;
        _interval = interval;
        _logger = logger;
        _timer = new Timer(Callback, null, interval(), Timeout.InfiniteTimeSpan);
    }

    private async void Callback(object? state)
    {
        try
        {
            await _action();
        }
        catch (Exception e)
        {
            // Swallow exception to prevent async void from crashing the process.
            // Log unhandled exceptions here as a safeguard; calling code may have its own exception handling.
            _logger?.LogError(e, "Unhandled exception in scheduled timer action");
        }
        finally
        {
            try
            {
                _timer.Change(_interval(), Timeout.InfiniteTimeSpan);
            }
            catch (ObjectDisposedException)
            {
                // Timer was disposed, ignore.
            }
        }
    }

    public void Dispose()
    {
        _timer.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _timer.DisposeAsync();
    }
}