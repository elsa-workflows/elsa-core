using Elsa.Scheduling.Contracts;
using Elsa.Scheduling.Models;
using Elsa.Scheduling.ScheduledTasks;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Scheduling.Schedules;

/// <summary>
/// A CRON schedule.
/// </summary>
public class CronSchedule : ISchedule
{
    public CronSchedule(string cronExpression)
    {
        CronExpression = cronExpression;
    }

    /// <summary>
    /// The CRON expression.
    /// </summary>
    public string CronExpression { get; init; } = default!;

    /// <summary>
    /// Creates a <see cref="ScheduledCronTask"/>.
    /// </summary>
    public IScheduledTask Schedule(ScheduleContext context) =>
        ActivatorUtilities.CreateInstance<ScheduledCronTask>(context.ServiceProvider, context.Task, CronExpression);
}