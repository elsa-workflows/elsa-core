using Elsa.Scheduling.Models;

namespace Elsa.Scheduling.Contracts;

/// <summary>
/// A schedule that can be used to schedule a task.
/// </summary>
public interface ISchedule
{
    IScheduledTask Schedule(ScheduleContext context);
}