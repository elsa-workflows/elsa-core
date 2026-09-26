using Elsa.Scheduling.Quartz.Options;
using Quartz;

namespace Elsa.Scheduling.Quartz.Models;

/// <summary>
/// Describes a single retry of a failed Quartz job. Passed to a custom delay generator so that applications can
/// override the delay computed by the configured backoff policy.
/// </summary>
public class QuartzJobRetryContext
{
    /// <summary>
    /// The execution context of the attempt that just failed.
    /// </summary>
    public required IJobExecutionContext JobExecutionContext { get; init; }

    /// <summary>
    /// The exception that caused the job to fail.
    /// </summary>
    public required Exception Exception { get; init; }

    /// <summary>
    /// The one-based number of the retry that is about to be scheduled. The first retry after the initial failure is
    /// attempt 1.
    /// </summary>
    public required int AttemptNumber { get; init; }

    /// <summary>
    /// The maximum number of retries that will be scheduled, as configured by <see cref="QuartzJobOptions.MaxRetryAttempts"/>.
    /// </summary>
    public required int MaxRetryAttempts { get; init; }

    /// <summary>
    /// The delay computed by the configured backoff policy. A custom delay generator can return this value to keep the
    /// computed delay, or <c>null</c> to fall back to it.
    /// </summary>
    public required TimeSpan ComputedDelay { get; init; }

    /// <summary>
    /// The key of the job that failed.
    /// </summary>
    public JobKey JobKey => JobExecutionContext.JobDetail.Key;
}
