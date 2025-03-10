using Cronos;

namespace Elsa.Common.RecurringTasks;

public class CronSchedule(ISystemClock systemClock, CronExpression expression) : ISchedule
{
    public ScheduledTimer CreateTimer(Func<Task> action)
    {
        return new ScheduledTimer(action, () => expression.GetNextOccurrence(systemClock.UtcNow.DateTime)!.Value - systemClock.UtcNow.DateTime);
    }
}