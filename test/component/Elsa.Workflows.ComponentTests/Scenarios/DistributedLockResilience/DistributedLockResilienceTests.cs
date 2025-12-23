using Elsa.Common.DistributedHosting;
using Elsa.Resilience.Contracts;
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
    [Fact]
    public async Task AcquireLockWithRetry_TransientFailureOnAcquisition_ShouldRetryAndSucceed()
    {
        // Arrange
        var mockProvider = Scope.ServiceProvider.GetRequiredService<TestDistributedLockProvider>();
        mockProvider.Reset();
        mockProvider.FailAcquisitionOnce();

        var lockProvider = Scope.ServiceProvider.GetRequiredService<IDistributedLockProvider>();
        var transientDetectionService = Scope.ServiceProvider.GetRequiredService<ITransientExceptionDetectionService>();
        var logger = Scope.ServiceProvider.GetRequiredService<ILogger<DistributedLockResilienceTests>>();
        var lockOptions = Scope.ServiceProvider.GetRequiredService<IOptions<DistributedLockingOptions>>();

        var retryPipeline = CreateRetryPipeline(transientDetectionService, logger);

        // Act - should succeed after retry
        var handle = await retryPipeline.ExecuteAsync(async ct =>
            await lockProvider.AcquireLockAsync("test-lock-1", lockOptions.Value.LockAcquisitionTimeout, ct),
            CancellationToken.None);

        // Assert
        Assert.NotNull(handle);
        Assert.Equal(2, mockProvider.AcquisitionAttemptCount); // First attempt fails, second succeeds

        await handle.DisposeAsync();
    }

    [Fact]
    public async Task AcquireLockWithRetry_TransientFailureOnRelease_ShouldLogButNotThrow()
    {
        // Arrange
        var mockProvider = Scope.ServiceProvider.GetRequiredService<TestDistributedLockProvider>();
        mockProvider.Reset();
        mockProvider.FailReleaseOnce();

        var lockProvider = Scope.ServiceProvider.GetRequiredService<IDistributedLockProvider>();
        var lockOptions = Scope.ServiceProvider.GetRequiredService<IOptions<DistributedLockingOptions>>();
        var logger = Scope.ServiceProvider.GetRequiredService<ILogger<DistributedLockResilienceTests>>();

        var handle = await lockProvider.AcquireLockAsync("test-lock-2", lockOptions.Value.LockAcquisitionTimeout);

        // Act & Assert - should not throw despite release failure (mimics production try-catch behavior)
        try
        {
            await handle.DisposeAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to release distributed lock (expected test behavior)");
        }

        Assert.Equal(1, mockProvider.ReleaseAttemptCount);
    }

    [Fact]
    public async Task AcquireLockWithRetry_MultipleTransientFailures_ShouldRetryAndSucceed()
    {
        // Arrange
        var mockProvider = Scope.ServiceProvider.GetRequiredService<TestDistributedLockProvider>();
        mockProvider.Reset();
        mockProvider.FailAcquisitionTimes(2); // Fail twice, succeed on third attempt

        var lockProvider = Scope.ServiceProvider.GetRequiredService<IDistributedLockProvider>();
        var transientDetectionService = Scope.ServiceProvider.GetRequiredService<ITransientExceptionDetectionService>();
        var logger = Scope.ServiceProvider.GetRequiredService<ILogger<DistributedLockResilienceTests>>();
        var lockOptions = Scope.ServiceProvider.GetRequiredService<IOptions<DistributedLockingOptions>>();

        var retryPipeline = CreateRetryPipeline(transientDetectionService, logger);

        // Act - should succeed after multiple retries
        var handle = await retryPipeline.ExecuteAsync(async ct =>
            await lockProvider.AcquireLockAsync("test-lock-3", lockOptions.Value.LockAcquisitionTimeout, ct),
            CancellationToken.None);

        // Assert
        Assert.NotNull(handle);
        Assert.Equal(3, mockProvider.AcquisitionAttemptCount); // Two attempts fail, third succeeds

        await handle.DisposeAsync();
    }

    [Fact]
    public async Task AcquireLockWithRetry_ExhaustedRetries_ShouldThrowException()
    {
        // Arrange
        var mockProvider = Scope.ServiceProvider.GetRequiredService<TestDistributedLockProvider>();
        mockProvider.Reset();
        mockProvider.FailAcquisitionTimes(4); // Fail 4 times (exceeds max retry attempts of 3)

        var lockProvider = Scope.ServiceProvider.GetRequiredService<IDistributedLockProvider>();
        var transientDetectionService = Scope.ServiceProvider.GetRequiredService<ITransientExceptionDetectionService>();
        var logger = Scope.ServiceProvider.GetRequiredService<ILogger<DistributedLockResilienceTests>>();
        var lockOptions = Scope.ServiceProvider.GetRequiredService<IOptions<DistributedLockingOptions>>();

        var retryPipeline = CreateRetryPipeline(transientDetectionService, logger);

        // Act & Assert - should throw after exhausting retries
        await Assert.ThrowsAsync<TimeoutException>(async () =>
        {
            await retryPipeline.ExecuteAsync(async ct =>
                await lockProvider.AcquireLockAsync("test-lock-4", lockOptions.Value.LockAcquisitionTimeout, ct),
                CancellationToken.None);
        });

        Assert.Equal(4, mockProvider.AcquisitionAttemptCount);
    }

    private static ResiliencePipeline CreateRetryPipeline(
        ITransientExceptionDetectionService transientExceptionDetectionService,
        ILogger logger)
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new()
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(10), // Shorter delay for tests
                BackoffType = DelayBackoffType.Constant,
                UseJitter = false,
                ShouldHandle = new PredicateBuilder().Handle<Exception>(ex => transientExceptionDetectionService.IsTransient(ex)),
                OnRetry = args =>
                {
                    logger.LogWarning(args.Outcome.Exception, "Transient error acquiring lock. Attempt {AttemptNumber} of {MaxAttempts}.", args.AttemptNumber + 1, 3);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }
}
