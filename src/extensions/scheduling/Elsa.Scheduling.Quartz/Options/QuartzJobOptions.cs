using Elsa.Scheduling.Quartz.Models;

namespace Elsa.Scheduling.Quartz.Options;

/// <summary>
/// Options for Quartz job execution behavior, including the retry policy applied to jobs that fail with a retryable
/// exception. Every retry is scheduled as a new Quartz trigger, so a pending retry never occupies a Quartz worker
/// thread while waiting, and it survives an application restart when Quartz is configured with a persistent job
/// store (with the default in-memory store, pending retries are lost on restart).
/// </summary>
public class QuartzJobOptions
{
    /// <summary>
    /// Whether failed jobs are retried at all. Defaults to <c>true</c>. When <c>false</c>, a failed job is logged and
    /// abandoned instead of being rescheduled.
    /// </summary>
    public bool RetryEnabled { get; set; } = true;

    /// <summary>
    /// The maximum number of retries scheduled for a single failing job. Defaults to <c>5</c>. Once this number of
    /// retries has been made, the job is abandoned. A value of zero or less disables retries.
    /// </summary>
    /// <remarks>
    /// Before this change, failed jobs retried indefinitely with a fixed 10-second delay. Now, retries stop after
    /// <see cref="MaxRetryAttempts"/> attempts (5 by default, roughly 15 seconds of exponential backoff). Deployments
    /// that relied on retrying through longer outages should raise <see cref="MaxRetryAttempts"/> and/or
    /// <see cref="MaxRetryDelay"/> when upgrading.
    /// </remarks>
    public int MaxRetryAttempts { get; set; } = 5;

    /// <summary>
    /// The delay before the first retry, and the base value the backoff strategy multiplies. Defaults to 500
    /// milliseconds. A value of zero or less makes every retry fire immediately.
    /// </summary>
    public TimeSpan InitialRetryDelay { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// The upper bound of the computed retry delay. Defaults to one minute. A value of zero or less removes the cap.
    /// </summary>
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// The backoff strategy used to grow the delay between retries. Defaults to
    /// <see cref="RetryBackoffType.Exponential"/>.
    /// </summary>
    public RetryBackoffType BackoffType { get; set; } = RetryBackoffType.Exponential;

    /// <summary>
    /// Whether to apply jitter to the computed delay, spreading retries of jobs that failed at the same time. Defaults
    /// to <c>true</c>. Jitter multiplies the computed delay by a uniformly random factor between 0.8 and 1.2, before
    /// <see cref="MaxRetryDelay"/> is applied.
    /// </summary>
    public bool UseJitter { get; set; } = true;

    /// <summary>
    /// An optional delegate that overrides the computed retry delay. Return <c>null</c> to fall back to the delay
    /// computed from <see cref="BackoffType"/>, <see cref="InitialRetryDelay"/>, <see cref="MaxRetryDelay"/> and
    /// <see cref="UseJitter"/>. Defaults to <c>null</c>, meaning the computed delay is used. A negative delay is
    /// treated as zero.
    /// </summary>
    public Func<QuartzJobRetryContext, TimeSpan?>? DelayGenerator { get; set; }

    /// <summary>
    /// An optional delegate that decides whether an exception is worth retrying. When set, it fully replaces the
    /// <see cref="Elsa.Resilience.ITransientExceptionDetector"/>: exceptions for which it returns <c>false</c> are
    /// treated as permanent failures. Defaults to <c>null</c>, meaning the transient exception detector decides.
    /// </summary>
    public Func<Exception, bool>? IsRetryable { get; set; }

    /// <summary>
    /// The delay before rescheduling a job after a transient failure.
    /// </summary>
    /// <remarks>
    /// Reads and writes <see cref="InitialRetryDelay"/>, so existing configuration keeps working. Note that the delay
    /// is now the starting point of the configured backoff strategy rather than a fixed delay.
    /// </remarks>
    [Obsolete("Use InitialRetryDelay instead. This property gets and sets the same value.")]
    public TimeSpan TransientExceptionRetryDelay
    {
        get => InitialRetryDelay;
        set => InitialRetryDelay = value;
    }
}
