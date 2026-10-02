using Elsa.Scheduling.Quartz.Options;
using Elsa.Scheduling.Quartz.Services;

namespace Elsa.Scheduling.Quartz.UnitTests.Services;

public class QuartzRetryDelayCalculatorTests
{
    private static readonly TimeSpan Initial = TimeSpan.FromSeconds(1);

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 4)]
    [InlineData(4, 8)]
    public void CalculateDelay_Exponential_DoublesEveryAttempt(int attempt, int expectedSeconds)
    {
        var delay = Calculate(attempt, RetryBackoffType.Exponential);

        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), delay);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 3)]
    [InlineData(4, 4)]
    public void CalculateDelay_Linear_GrowsByTheInitialDelay(int attempt, int expectedSeconds)
    {
        var delay = Calculate(attempt, RetryBackoffType.Linear);

        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), delay);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(10)]
    public void CalculateDelay_Constant_AlwaysReturnsTheInitialDelay(int attempt)
    {
        var delay = Calculate(attempt, RetryBackoffType.Constant);

        Assert.Equal(Initial, delay);
    }

    [Theory]
    [InlineData(RetryBackoffType.Exponential)]
    [InlineData(RetryBackoffType.Linear)]
    [InlineData(RetryBackoffType.Constant)]
    public void CalculateDelay_FirstAttempt_ReturnsTheInitialDelay(RetryBackoffType backoffType)
    {
        var delay = Calculate(1, backoffType);

        Assert.Equal(Initial, delay);
    }

    [Fact]
    public void CalculateDelay_GrowthBeyondTheMaximum_IsCapped()
    {
        var maxRetryDelay = TimeSpan.FromSeconds(30);
        var delay = Calculate(10, RetryBackoffType.Exponential, options => options.MaxRetryDelay = maxRetryDelay);

        Assert.Equal(maxRetryDelay, delay);
    }

    [Fact]
    public void CalculateDelay_ExtremeAttemptNumber_DoesNotOverflow()
    {
        var delay = Calculate(2000, RetryBackoffType.Exponential, options => options.MaxRetryDelay = TimeSpan.FromMinutes(1));

        Assert.Equal(TimeSpan.FromMinutes(1), delay);
    }

    [Theory]
    [InlineData(0.0, 0.8)]
    [InlineData(0.5, 1.0)]
    [InlineData(0.9999, 1.2)]
    public void CalculateDelay_WithJitter_ScalesTheDelayByTheRandomFactor(double randomValue, double expectedFactor)
    {
        var options = QuartzJobOptionsFor(RetryBackoffType.Constant, o => o.UseJitter = true);
        var calculator = new QuartzRetryDelayCalculator(new FixedRandom(randomValue));

        var delay = calculator.CalculateDelay(1, options);

        Assert.Equal(Initial.Ticks * expectedFactor, delay.Ticks, Initial.Ticks * 0.001);
    }

    [Fact]
    public void CalculateDelay_WithJitter_StaysWithinTheJitterBounds()
    {
        var options = QuartzJobOptionsFor(RetryBackoffType.Exponential, o => o.UseJitter = true);
        var calculator = new QuartzRetryDelayCalculator(new Random(1234));

        for (var attempt = 1; attempt <= 5; attempt++)
        {
            var expected = Initial.Ticks * Math.Pow(2, attempt - 1);
            var delay = calculator.CalculateDelay(attempt, options);

            Assert.InRange(delay.Ticks, (long)(expected * QuartzRetryDelayCalculator.MinJitterFactor), (long)(expected * QuartzRetryDelayCalculator.MaxJitterFactor));
        }
    }

    [Fact]
    public void CalculateDelay_WithJitter_IsCappedAfterJitterIsApplied()
    {
        var options = QuartzJobOptionsFor(RetryBackoffType.Constant, o =>
        {
            o.UseJitter = true;
            o.MaxRetryDelay = Initial;
        });
        var calculator = new QuartzRetryDelayCalculator(new FixedRandom(0.9999));

        var delay = calculator.CalculateDelay(1, options);

        Assert.Equal(Initial, delay);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CalculateDelay_AttemptNumberBelowOne_IsTreatedAsTheFirstAttempt(int attempt)
    {
        var delay = Calculate(attempt, RetryBackoffType.Exponential);

        Assert.Equal(Initial, delay);
    }

    [Fact]
    public void CalculateDelay_NonPositiveInitialDelay_ReturnsZero()
    {
        var delay = Calculate(3, RetryBackoffType.Exponential, options => options.InitialRetryDelay = TimeSpan.Zero);

        Assert.Equal(TimeSpan.Zero, delay);
    }

    [Fact]
    public void CalculateDelay_NonPositiveMaximumDelay_DoesNotCap()
    {
        var delay = Calculate(3, RetryBackoffType.Exponential, options => options.MaxRetryDelay = TimeSpan.Zero);

        Assert.Equal(TimeSpan.FromSeconds(4), delay);
    }

    private static TimeSpan Calculate(int attempt, RetryBackoffType backoffType, Action<QuartzJobOptions>? configure = null) =>
        new QuartzRetryDelayCalculator().CalculateDelay(attempt, QuartzJobOptionsFor(backoffType, configure));

    private static QuartzJobOptions QuartzJobOptionsFor(RetryBackoffType backoffType, Action<QuartzJobOptions>? configure = null)
    {
        var options = new QuartzJobOptions
        {
            InitialRetryDelay = Initial,
            MaxRetryDelay = TimeSpan.FromDays(1),
            BackoffType = backoffType,
            UseJitter = false
        };

        configure?.Invoke(options);
        return options;
    }

    /// <summary>
    /// A random source that always yields the same value, making jitter deterministic.
    /// </summary>
    private sealed class FixedRandom(double value) : Random
    {
        public override double NextDouble() => value;
    }
}
