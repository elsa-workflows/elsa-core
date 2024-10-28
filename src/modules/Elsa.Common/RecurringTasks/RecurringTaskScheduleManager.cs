namespace Elsa.Common.RecurringTasks;

public class RecurringTaskScheduleManager
{
    public IDictionary<Type, ISchedule> ScheduledTasks { get; set; } = new Dictionary<Type, ISchedule>();
    
    public void ConfigureScheduledTask<T>(ISchedule schedule) where T : IRecurringTask
    {
        ConfigureScheduledTask(typeof(T), schedule);
    }
    
    public void ConfigureScheduledTask(Type recurringTaskType, ISchedule schedule)
    {
        ScheduledTasks[recurringTaskType] = schedule;
    }

    public ISchedule GetScheduleFor(Type taskType)
    {
        return ScheduledTasks.TryGetValue(taskType, out var schedule) ? schedule : new IntervalSchedule(TimeSpan.FromMinutes(1));
    }
}