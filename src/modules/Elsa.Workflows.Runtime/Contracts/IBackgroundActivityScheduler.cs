using Elsa.Workflows.Runtime.Models;

namespace Elsa.Workflows.Runtime.Contracts;

/// <summary>
/// Invokes activities from a background worker within the context of its workflow instance.
/// </summary>
public interface IBackgroundActivityScheduler
{
    /// <summary>
    /// Schedules the specified activity for execution in the background.
    /// </summary>
    /// <param name="scheduledBackgroundActivity">The activity to schedule fo background execution.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A handle representing the asynchronous invocation.</returns>
    Task<string> ScheduleAsync(ScheduledBackgroundActivity scheduledBackgroundActivity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels the specified job.
    /// </summary>
    /// <param name="jobId">the ID of the job to cancel.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task CancelAsync(string jobId, CancellationToken cancellationToken = default);
}