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
        await _action();
        _timer.Change(_interval(), Timeout.InfiniteTimeSpan);
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