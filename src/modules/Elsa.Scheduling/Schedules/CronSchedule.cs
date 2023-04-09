using Elsa.Scheduling.Contracts;
using Elsa.Scheduling.Models;
using Elsa.Scheduling.ScheduledTasks;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Scheduling.Schedules;

/// <summary>
/// A CRON schedule.
/// </summary>
[PublicAPI]
public class CronSchedule : ISchedule
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CronSchedule"/> class.
    /// </summary>
    /// <param name="cronExpression">The CRON expression.</param>
    public CronSchedule(string cronExpression)
    {
        CronExpression = cronExpression;
    }

    /// <summary>
    /// The CRON expression.
    /// </summary>
    public string CronExpression { get; init; }

    /// <summary>
    /// Creates a <see cref="ScheduledCronTask"/>.
    /// </summary>
    public IScheduledTask Schedule(ScheduleContext context) =>
        ActivatorUtilities.CreateInstance<ScheduledCronTask>(context.ServiceProvider, context.Task, CronExpression);
}