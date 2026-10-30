using Elsa.Scheduling.Quartz.Options;

namespace Elsa.Scheduling.Quartz.Contracts;

/// <summary>
/// Computes the delay before the next retry of a failed Quartz job.
/// </summary>
public interface IQuartzRetryDelayCalculator
{
    /// <summary>
    /// Computes the delay before the specified retry attempt.
    /// </summary>
    /// <param name="attemptNumber">The one-based number of the retry to compute the delay for. The first retry is attempt 1.</param>
    /// <param name="options">The options describing the backoff strategy to apply.</param>
    /// <returns>The delay to wait before the specified retry. Never negative.</returns>
    TimeSpan CalculateDelay(int attemptNumber, QuartzJobOptions options);
}
