using Cronos;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Elsa.Common.RecurringTasks;

[UsedImplicitly]
public class ConfigureRecurringTasksScheduleStartupTask(IOptions<RecurringTaskOptions> options, ISystemClock systemClock, RecurringTaskScheduleManager recurringTaskScheduleManager) : IStartupTask
{
    public Task ExecuteAsync(CancellationToken cancellationToken)
    {
        foreach(var entry in options.Value.Schedule.ScheduledTasks)
        {
            var taskType = entry.Key;
            var intervalExpression = entry.Value;
            var schedule = intervalExpression.Type switch
            {
                IntervalExpressionType.Cron => (ISchedule)new CronSchedule(systemClock, CronExpression.Parse(intervalExpression.Expression)),
                IntervalExpressionType.Interval => new IntervalSchedule(TimeSpan.Parse(intervalExpression.Expression)),
                _ => throw new NotSupportedException($"Interval expression type '{intervalExpression.Type}' is not supported.")
            };
            recurringTaskScheduleManager.ConfigureScheduledTask(taskType, schedule);
        }
        return Task.CompletedTask;
    }
}