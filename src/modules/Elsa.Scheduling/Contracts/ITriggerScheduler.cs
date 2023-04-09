using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Scheduling.Contracts;

/// <summary>
/// Schedules tasks for the specified list of triggers.
/// </summary>
public interface ITriggerScheduler
{
    /// <summary>
    /// Schedules tasks for the specified list of triggers. 
    /// </summary>
    /// <param name="triggers">The triggers to schedule tasks for.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task ScheduleAsync(IEnumerable<StoredTrigger> triggers, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Unschedules tasks for the specified list of triggers.
    /// </summary>
    /// <param name="triggers">The triggers to unschedule tasks for.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task UnscheduleAsync(IEnumerable<StoredTrigger> triggers, CancellationToken cancellationToken = default);
}