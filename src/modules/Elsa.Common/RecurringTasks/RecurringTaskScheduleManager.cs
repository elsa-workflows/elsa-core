using Cronos;
using Microsoft.Extensions.Options;

namespace Elsa.Common.RecurringTasks;

public class RecurringTaskScheduleManager(IOptions<RecurringTaskOptions> options, ISystemClock systemClock)
{
    public IDictionary<Type, ISchedule> ScheduledTasks { get; set; } = new Dictionary<Type, ISchedule>();
    
    public ISchedule GetScheduleFor(Type taskType)
    {
        if (!ScheduledTasks.TryGetValue(taskType, out var schedule))
        {
            var intervalExpression = options.Value.Schedule.ScheduledTasks.TryGetValue(taskType, out var expr) ? expr : null;
            schedule = intervalExpression != null ? CreateSchedule(intervalExpression) : new IntervalSchedule(TimeSpan.FromMinutes(1));
            ScheduledTasks[taskType] = schedule;
        }

        return schedule;
    }
    
    private ISchedule CreateSchedule(IntervalExpression intervalExpression)
    {
        return intervalExpression.Type switch
        {
            IntervalExpressionType.Cron => new CronSchedule(systemClock, CronExpression.Parse(intervalExpression.Expression)),
            IntervalExpressionType.Interval => new IntervalSchedule(TimeSpan.Parse(intervalExpression.Expression)),
            _ => throw new NotSupportedException($"Interval expression type '{intervalExpression.Type}' is not supported.")
        };
    }
}