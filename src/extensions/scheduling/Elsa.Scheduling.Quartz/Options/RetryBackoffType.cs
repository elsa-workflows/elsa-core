namespace Elsa.Scheduling.Quartz.Options;

/// <summary>
/// The backoff strategy used to compute the delay between two retries of a failed Quartz job.
/// </summary>
public enum RetryBackoffType
{
    /// <summary>
    /// The delay doubles with every attempt: <c>InitialRetryDelay * 2^(attempt - 1)</c>. This is the default.
    /// </summary>
    Exponential = 0,

    /// <summary>
    /// The delay grows linearly with every attempt: <c>InitialRetryDelay * attempt</c>.
    /// </summary>
    Linear = 1,

    /// <summary>
    /// The delay is the same for every attempt: <c>InitialRetryDelay</c>.
    /// </summary>
    Constant = 2
}
