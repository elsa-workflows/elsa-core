namespace Elsa.Common.RecurringTasks;

public class RecurringTaskSchedule
{
    public IDictionary<Type, IntervalExpression> ScheduledTasks { get; } = new Dictionary<Type, IntervalExpression>();
    
    public void ConfigureTask<T>(TimeSpan interval) where T : IRecurringTask
    {
        ConfigureTask<T>(IntervalExpression.FromInterval(interval));
    }
    
    public void ConfigureTask<T>(string cronExpression) where T : IRecurringTask
    {
        ConfigureTask<T>(IntervalExpression.FromCron(cronExpression));
    }
    
    public void ConfigureTask<T>(IntervalExpression intervalExpression) where T : IRecurringTask
    {
        ConfigureTask(typeof(T), intervalExpression);
    }
    
    public void ConfigureTask(Type recurringTaskType, IntervalExpression intervalExpression)
    {
        ScheduledTasks[recurringTaskType] = intervalExpression;
    }
}