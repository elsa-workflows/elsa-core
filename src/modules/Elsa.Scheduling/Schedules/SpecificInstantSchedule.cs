using Elsa.Scheduling.Contracts;
using Elsa.Scheduling.Models;
using Elsa.Scheduling.ScheduledTasks;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Scheduling.Schedules;

/// <summary>
/// A schedule that can be used to schedule a task at a specific instant.
/// </summary>
[PublicAPI] 
public class SpecificInstantSchedule : ISchedule
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SpecificInstantSchedule"/> class.
    /// </summary>
    /// <param name="startAt">The date and time to schedule the task for.</param>
    public SpecificInstantSchedule(DateTimeOffset startAt)
    {
        StartAt = startAt;
    }

    /// <summary>
    /// The date and time to schedule the task for.
    /// </summary>
    public DateTimeOffset StartAt { get; init; }

    /// <inheritdoc />
    public IScheduledTask Schedule(ScheduleContext context)
    {
        return ActivatorUtilities.CreateInstance<ScheduledSpecificInstantTask>(context.ServiceProvider, context.Task, StartAt);
    }
}