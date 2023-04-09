using Elsa.Scheduling.Models;

namespace Elsa.Scheduling.Contracts;

/// <summary>
/// A schedule that can be used to schedule a task.
/// </summary>
public interface ISchedule
{
    /// <summary>
    /// Create a scheduled task.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns>The scheduled task.</returns>
    IScheduledTask Schedule(ScheduleContext context);
}