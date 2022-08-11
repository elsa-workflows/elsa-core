using System.Threading;
using Cronos;
using Elsa.Common.Services;
using Elsa.Jobs.Services;
using Timer = System.Timers.Timer;

namespace Elsa.Jobs.ScheduledJobs;

public class ScheduledCronJob : IScheduledJob
{
    private readonly IJob _job;
    private readonly ISystemClock _systemClock;
    private readonly IJobRunner _jobRunner;
    private readonly CronExpression _parsedCronExpression;
    private readonly CancellationToken _cancellationToken;
    private Timer? _timer;

    public ScheduledCronJob(string name, IJob job, ISystemClock systemClock, string cronExpression, IJobRunner jobRunner, CancellationToken cancellationToken)
    {
        Name = name;

        _job = job;
        _systemClock = systemClock;
        _parsedCronExpression = CronExpression.Parse(cronExpression);
        _jobRunner = jobRunner;
        _cancellationToken = cancellationToken;

        Schedule();
    }

    public string Name { get; set; }
    public void Cancel() => _timer?.Dispose();

    private void Schedule()
    {
        var now = _systemClock.UtcNow;
        var parsedCronExpression = _parsedCronExpression;
        var nextOccurence = parsedCronExpression.GetNextOccurrence(now.UtcDateTime)!;

        var delay = nextOccurence.Value - now;

        if (delay.Milliseconds <= 0)
        {
            Schedule();
            return;
        }

        _timer = new Timer(delay.TotalMilliseconds);

        _timer.Elapsed += async (_, _) =>
        {
            _timer.Dispose();
            _timer = null;

            if (!_cancellationToken.IsCancellationRequested) await _jobRunner.RunJobAsync(_job, _cancellationToken);
            if (!_cancellationToken.IsCancellationRequested) Schedule();
        };
    }
}