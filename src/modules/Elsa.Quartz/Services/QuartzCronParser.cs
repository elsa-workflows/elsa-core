using Elsa.Common.Contracts;
using Elsa.Scheduling.Contracts;
using Quartz;

namespace Elsa.Quartz.Services;

/// <summary>
/// A cron parser that uses Quartz.
/// </summary>
public class QuartzCronParser : ICronParser
{
    private readonly ISystemClock _systemClock;

    /// <summary>
    /// Initializes a new instance of the <see cref="QuartzCronParser"/> class.
    /// </summary>
    public QuartzCronParser(ISystemClock systemClock)
    {
        _systemClock = systemClock;
    }
    
    /// <inheritdoc />
    public DateTimeOffset GetNextOccurrence(string expression)
    {
        var schedule = new CronExpression(expression);
        var now = _systemClock.UtcNow;
        return schedule.GetNextValidTimeAfter(now).GetValueOrDefault();
    }
}