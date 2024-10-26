namespace Elsa.Common.RecurringTasks;

public class IntervalSchedule(TimeSpan interval) : ISchedule
{
    public ScheduledTimer CreateTimer(Func<Task> action)
    {
        return new ScheduledTimer(action, () => interval);
    }
}