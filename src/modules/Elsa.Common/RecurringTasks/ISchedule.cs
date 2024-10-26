namespace Elsa.Common.RecurringTasks;

public interface ISchedule
{
    ScheduledTimer CreateTimer(Func<Task> action);
}