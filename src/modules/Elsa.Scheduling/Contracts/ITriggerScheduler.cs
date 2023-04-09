using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Scheduling.Contracts;

/// <summary>
/// Schedules jobs for the specified list of workflow triggers.
/// </summary>
public interface ITriggerScheduler
{
    Task ScheduleAsync(IEnumerable<StoredTrigger> triggers, CancellationToken cancellationToken = default);
    Task UnscheduleAsync(IEnumerable<StoredTrigger> triggers, CancellationToken cancellationToken = default);
}