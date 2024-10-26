using Cronos;

namespace Elsa.Common.RecurringTasks;

public class RecurringTasksSchedule
{
    public IDictionary<Type, Schedule> ScheduledTasks { get; set; } = new Dictionary<Type, Schedule>();
    
    public void ConfigureScheduledTask<T>(TimeSpan interval) where T : IRecurringTask
    {
        ConfigureScheduledTask<T>(IntervalExpressionType.Interval, interval.ToString());
    }

    public void ConfigureScheduledTask<T>(CronExpression cronExpression) where T : IRecurringTask
    {
        ConfigureScheduledTask<T>(IntervalExpressionType.Cron, cronExpression.ToString());
    }

    public void ConfigureScheduledTask<T>(IntervalExpressionType type, string expression) where T : IRecurringTask
    {
        ConfigureScheduledTask<T>(new Schedule
        {
            Type = type,
            Expression = expression
        });
    }
    
    public void ConfigureScheduledTask<T>(Schedule expression) where T : IRecurringTask
    {
        ScheduledTasks[typeof(T)] = expression;
    }

    public Schedule GetScheduleFor(Type taskType)
    {
        return ScheduledTasks.TryGetValue(taskType, out var expression) ? expression : new Schedule
        {
            Type = IntervalExpressionType.Interval,
            Expression = TimeSpan.FromMinutes(1).ToString()
        };
    }
}