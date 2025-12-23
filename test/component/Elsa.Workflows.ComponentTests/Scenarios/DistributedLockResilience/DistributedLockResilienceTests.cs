using Elsa.Common.DistributedHosting;
using Elsa.Resilience;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.ComponentTests.Scenarios.DistributedLockResilience.Mocks;
using Medallion.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace Elsa.Workflows.ComponentTests.Scenarios.DistributedLockResilience;

public class DistributedLockResilienceTests(App app) : AppComponentTest(app)
{
    private const int MaxRetryAttempts = 3;
    
    private TestDistributedLockProvider MockProvider => Scope.ServiceProvider.GetRequiredService<TestDistributedLockProvider>();
    private ITransientExceptionDetector TransientExceptionDetector => Scope.ServiceProvider.GetRequiredService<ITransientExceptionDetector>();
    private ILogger<DistributedLockResilienceTests> Logger => Scope.ServiceProvider.GetRequiredService<ILogger<DistributedLockResilienceTests>>();
    private DistributedLockingOptions LockOptions => Scope.ServiceProvider.GetRequiredService<IOptions<DistributedLockingOptions>>().Value;
    private ResiliencePipeline RetryPipeline => CreateRetryPipeline(TransientExceptionDetector, Logger);

    [Theory]
    [InlineData(1, 2, false)] // Single failure, succeeds on retry
    [InlineData(2, 3, false)] // Two failures, succeeds on third attempt
    [InlineData(4, 4, true)]  // Four failures, exhausts retries (MaxRetryAttempts = 3)
    public async Task AcquireLockWithRetry_AcquisitionFailures_BehavesAsExpected(int failureCount, int expectedAttemptCount, bool shouldThrow)
    {
        // Arrange
        MockProvider.Reset();
        MockProvider.FailAcquisitionTimes(failureCount);

        // Act & Assert
        if (shouldThrow)
        {
            await Assert.ThrowsAsync<TimeoutException>(async () => await AcquireLockWithRetryAsync($"test-lock-{failureCount}"));
        }
        else
        {
            await using var handle = await AcquireLockWithRetryAsync($"test-lock-{failureCount}");
            Assert.NotNull(handle);
        }

        Assert.Equal(expectedAttemptCount, MockProvider.AcquisitionAttemptCount);
    }

    [Fact]
    public async Task AcquireLockWithRetry_TransientFailureOnRelease_ShouldLogButNotThrow()
    {
        // Arrange
        MockProvider.Reset();
        MockProvider.FailReleaseOnce();

        // Act & Assert - mimics production try-catch behavior where release exceptions are logged but not thrown
        await using var handle = await MockProvider.AcquireLockAsync("test-lock-release", LockOptions.LockAcquisitionTimeout);
        await SafeDisposeAsync(handle);

        Assert.Equal(1, MockProvider.ReleaseAttemptCount);
    }

    private async Task<IDistributedSynchronizationHandle?> AcquireLockWithRetryAsync(string lockName) =>
        await RetryPipeline.ExecuteAsync(async ct =>
            await MockProvider.AcquireLockAsync(lockName, LockOptions.LockAcquisitionTimeout, ct),
            CancellationToken.None);

    private async Task SafeDisposeAsync(IDistributedSynchronizationHandle handle)
    {
        try
        {
            await handle.DisposeAsync();
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to release distributed lock (expected test behavior)");
        }
    }

    private static ResiliencePipeline CreateRetryPipeline(ITransientExceptionDetector transientExceptionDetector, ILogger logger) =>
        new ResiliencePipelineBuilder()
            .AddRetry(new()
            {
                MaxRetryAttempts = MaxRetryAttempts,
                // NOTE: The test retry policy intentionally differs from the production configuration.
                // - We use a short, constant delay (10ms) to keep tests fast.
                // - We disable jitter and exponential backoff to make timing deterministic and assertions stable.
                //   The production pipeline uses a larger delay with exponential backoff and jitter for robustness.
                Delay = TimeSpan.FromMilliseconds(10),
                BackoffType = DelayBackoffType.Constant,
                UseJitter = false,
                ShouldHandle = new PredicateBuilder().Handle<Exception>(transientExceptionDetector.IsTransient),
                OnRetry = args =>
                {
                    logger.LogWarning(args.Outcome.Exception, "Transient error acquiring lock. Attempt {AttemptNumber} of {MaxAttempts}.", args.AttemptNumber + 1, MaxRetryAttempts);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
}
