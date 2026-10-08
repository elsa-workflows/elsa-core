using Elsa.Scheduling.Quartz.Contracts;
using Elsa.Scheduling.Quartz.Options;

namespace Elsa.Scheduling.Quartz.Services;

/// <summary>
/// Default implementation of <see cref="IQuartzRetryDelayCalculator"/>. Computes a backoff delay with optional jitter,
/// following the same semantics as Polly's retry strategies, but without pinning a thread while waiting.
/// </summary>
/// <param name="random">
/// The random source used to compute jitter. Defaults to <see cref="System.Random.Shared"/>, which is thread-safe.
/// Pass a seeded instance to make jitter deterministic in tests; such an instance must not be shared across threads.
/// </param>
public class QuartzRetryDelayCalculator(Random? random = null) : IQuartzRetryDelayCalculator
{
    /// <summary>
    /// The lowest factor jitter can multiply the computed delay by.
    /// </summary>
    public const double MinJitterFactor = 0.8;

    /// <summary>
    /// The highest factor jitter can multiply the computed delay by.
    /// </summary>
    public const double MaxJitterFactor = 1.2;

    private readonly Random _random = random ?? Random.Shared;

    /// <inheritdoc />
    public TimeSpan CalculateDelay(int attemptNumber, QuartzJobOptions options)
    {
        var attempt = Math.Max(1, attemptNumber);
        double initialTicks = options.InitialRetryDelay.Ticks;

        if (initialTicks <= 0)
            return TimeSpan.Zero;

        var multiplier = options.BackoffType switch
        {
            RetryBackoffType.Linear => attempt,
            RetryBackoffType.Constant => 1d,
            _ => Math.Pow(2, attempt - 1)
        };

        var ticks = initialTicks * multiplier;

        if (options.UseJitter)
            ticks *= MinJitterFactor + _random.NextDouble() * (MaxJitterFactor - MinJitterFactor);

        // A non-positive maximum removes the cap; the delay is then bounded only by TimeSpan itself.
        double maxTicks = options.MaxRetryDelay.Ticks;

        if (maxTicks > 0 && ticks >= maxTicks)
            return options.MaxRetryDelay;

        return ticks >= long.MaxValue ? TimeSpan.MaxValue : TimeSpan.FromTicks((long)ticks);
    }
}
