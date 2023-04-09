using Elsa.Scheduling.Contracts;
using Elsa.Scheduling.Models;
using Elsa.Scheduling.ScheduledTasks;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Scheduling.Schedules;

/// <summary>
/// A recurring schedule.
/// </summary>
[PublicAPI]
public class RecurringSchedule : ISchedule
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RecurringSchedule"/> class.
    /// </summary>
    /// <param name="startAt">The time at which the first occurrence should occur.</param>
    /// <param name="interval">The interval between occurrences.</param>
    public RecurringSchedule(DateTimeOffset startAt, TimeSpan interval)
    {
        StartAt = startAt;
        Interval = interval;
    }

    /// <summary>
    /// The time at which the first occurrence should occur.
    /// </summary>
    public DateTimeOffset StartAt { get; init; }

    /// <summary>
    /// The interval between occurrences.
    /// </summary>
    public TimeSpan Interval { get; init; }

    /// <inheritdoc />
    public IScheduledTask Schedule(ScheduleContext context) =>
        ActivatorUtilities.CreateInstance<ScheduledRecurringTask>(context.ServiceProvider, context.Task, StartAt, Interval);
}