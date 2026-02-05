namespace Elsa.Common.RecurringTasks;

public class ScheduledTimer : IDisposable, IAsyncDisposable
{
    private readonly Func<Task> _action;
    private readonly Func<TimeSpan> _interval;
    private readonly Timer _timer;

    public ScheduledTimer(Func<Task> action, Func<TimeSpan> interval)
    {
        _action = action;
        _interval = interval;
        _timer = new Timer(Callback, null, interval(), Timeout.InfiniteTimeSpan);
    }

    private async void Callback(object? state)
    {
        try
        {
            try
            {
                await _action();
            }
            catch (Exception)
            {
                // The action failed, but we still want to reschedule the timer.
            }
            
            _timer.Change(_interval(), Timeout.InfiniteTimeSpan);
        }
        catch (Exception)
        {
            // Ignore exceptions to prevent process crash (async void).
            // This is especially important during application shutdown where the timer or dependencies might be disposed.
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