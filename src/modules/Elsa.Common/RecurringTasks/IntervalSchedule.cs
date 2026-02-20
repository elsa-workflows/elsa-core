using Microsoft.Extensions.Logging;

namespace Elsa.Common.RecurringTasks;

public class IntervalSchedule(TimeSpan interval) : ISchedule
{
    public ScheduledTimer CreateTimer(Func<Task> action, ILogger? logger = null)
    {
        return new ScheduledTimer(action, () => interval, logger);
    }
}