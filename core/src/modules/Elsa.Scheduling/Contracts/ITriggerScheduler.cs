using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Scheduling;

/// <summary>
/// Schedules tasks for the specified list of triggers.
/// </summary>
/// <remarks>
/// An implementation selects the triggers it schedules by name, and adds those names to
/// <see cref="Elsa.Workflows.Runtime.Options.RuntimeOptions.ScheduledTriggerNames"/>: only triggers with these names are loaded for scheduling,
/// and a tenant-agnostic workflow definition stores them once per tenant.
/// </remarks>
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