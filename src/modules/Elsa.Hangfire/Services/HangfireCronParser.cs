using Elsa.Common.Contracts;
using Elsa.Scheduling.Contracts;
using NCrontab;

namespace Elsa.Hangfire.Services;

/// <summary>
/// A cron parser that uses Hangfire.
/// </summary>
public class HangfireCronParser : ICronParser
{
    private readonly ISystemClock _systemClock;

    /// <summary>
    /// Initializes a new instance of the <see cref="HangfireCronParser"/> class.
    /// </summary>
    public HangfireCronParser(ISystemClock systemClock)
    {
        _systemClock = systemClock;
    }

    /// <inheritdoc />
    public DateTimeOffset GetNextOccurrence(string expression)
    {
        var schedule = CrontabSchedule.Parse(expression, new CrontabSchedule.ParseOptions { IncludingSeconds = true });
        var now = _systemClock.UtcNow;
        return schedule.GetNextOccurrence(now.DateTime);
    }
}