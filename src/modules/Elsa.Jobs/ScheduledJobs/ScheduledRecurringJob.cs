using Elsa.Common.Services;
using Elsa.Jobs.Services;
using Timer = System.Timers.Timer;

namespace Elsa.Jobs.ScheduledJobs;

public class ScheduledRecurringJob : IScheduledJob
{
    private readonly IJob _job;
    private readonly ISystemClock _systemClock;
    private readonly TimeSpan _interval;
    private readonly IJobRunner _jobRunner;
    private readonly CancellationToken _cancellationToken;
    private DateTimeOffset _startAt;
    private Timer? _timer;

    public ScheduledRecurringJob(string name, IJob job, ISystemClock systemClock, DateTimeOffset startAt, TimeSpan interval, IJobRunner jobRunner, CancellationToken cancellationToken)
    {
        Name = name;
        _job = job;
        _systemClock = systemClock;
        _startAt = startAt;
        _interval = interval;
        _jobRunner = jobRunner;
        _cancellationToken = cancellationToken;

        Schedule();
    }

    public string Name { get; set; }
    public void Cancel() => _timer?.Dispose();

    private void Schedule()
    {
        var now = _systemClock.UtcNow;
        var delay = _startAt - now;

        if (delay.Milliseconds <= 0)
        {
            Schedule();
            return;
        }

        _timer = new Timer(delay.TotalMilliseconds) { Enabled = true };

        _timer.Elapsed += async (_, _) =>
        {
            _timer.Dispose();
            _timer = null;
            _startAt = now + _interval;

            if (!_cancellationToken.IsCancellationRequested) await _jobRunner.RunJobAsync(_job, _cancellationToken);
            if (!_cancellationToken.IsCancellationRequested) Schedule();
        };
    }
}