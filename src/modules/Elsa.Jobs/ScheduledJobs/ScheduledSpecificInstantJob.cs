using Elsa.Common.Contracts;
using Elsa.Jobs.Contracts;
using Timer = System.Timers.Timer;

namespace Elsa.Jobs.ScheduledJobs;

public class ScheduledSpecificInstantJob : IScheduledJob
{
    private readonly IJob _job;
    private readonly ISystemClock _systemClock;
    private readonly DateTimeOffset _startAt;
    private readonly IJobRunner _jobRunner;
    private readonly CancellationToken _cancellationToken;
    private Timer? _timer;

    public ScheduledSpecificInstantJob(string name, IJob job, ISystemClock systemClock, DateTimeOffset startAt, IJobRunner jobRunner, CancellationToken cancellationToken)
    {
        _job = job;
        _systemClock = systemClock;
        _startAt = startAt;
        _jobRunner = jobRunner;
        _cancellationToken = cancellationToken;
        Name = name;

        Schedule();
    }

    public string Name { get; set; }
    public void Cancel() => _timer?.Dispose();

    private void Schedule()
    {
        var now = _systemClock.UtcNow;
        var delay = _startAt - now;

        if (delay.Milliseconds <= 0)
            delay = TimeSpan.FromMilliseconds(1);

        _timer = new Timer(delay.TotalMilliseconds) { Enabled = true };

        _timer.Elapsed += async (_, _) =>
        {
            _timer?.Dispose();
            _timer = null;

            if (!_cancellationToken.IsCancellationRequested) await _jobRunner.RunJobAsync(_job, _cancellationToken);
        };
    }
}