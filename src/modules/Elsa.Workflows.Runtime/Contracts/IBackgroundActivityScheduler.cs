namespace Elsa.Workflows.Runtime;

/// <summary>
/// Invokes activities from a background worker within the context of its workflow instance.
/// </summary>
public interface IBackgroundActivityScheduler
{
    /// <summary>
    /// Creates a scheduled job for the specified activity for execution in the background without starting the job.
    /// The caller is responsible for starting the job.
    /// </summary>
    /// <param name="scheduledBackgroundActivity">The activity to schedule fo background execution.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A handle representing the asynchronous invocation.</returns>
    Task<string> CreateAsync(ScheduledBackgroundActivity scheduledBackgroundActivity, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Scheduled the specified job.
    /// </summary>
    Task ScheduleAsync(string jobId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Schedules the specified activity for execution in the background.
    /// </summary>
    /// <param name="scheduledBackgroundActivity">The activity to schedule fo background execution.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A handle representing the asynchronous invocation.</returns>
    Task<string> ScheduleAsync(ScheduledBackgroundActivity scheduledBackgroundActivity, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Removes the specified job from the schedule.
    /// </summary>
    Task UnscheduledAsync(string jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels the specified job.
    /// </summary>
    /// <param name="jobId">the ID of the job to cancel.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task CancelAsync(string jobId, CancellationToken cancellationToken = default);
}