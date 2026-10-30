using Quartz;

namespace Elsa.Scheduling.Quartz.Contracts;

/// <summary>
/// Serializes mutations of one original Quartz schedule and its derived retry triggers across application nodes.
/// </summary>
public interface IQuartzScheduleCoordinator
{
    /// <summary>
    /// Executes a schedule mutation while holding the lock for the original trigger key.
    /// </summary>
    /// <param name="originalTriggerKey">The original schedule key shared by all retry triggers in the chain.</param>
    /// <param name="action">The mutation to execute while the lock is held.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    Task ExecuteAsync(TriggerKey originalTriggerKey, Func<CancellationToken, Task> action, CancellationToken cancellationToken = default);
}
