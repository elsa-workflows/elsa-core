using Elsa.Common.DistributedHosting;
using Elsa.Common.Models;
using Elsa.Resilience;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.ComponentTests.Scenarios.DistributedLockResilience.Mocks;
using Elsa.Workflows.ComponentTests.Scenarios.DistributedLockResilience.Workflows;
using Elsa.Workflows.Management;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;
using Medallion.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace Elsa.Workflows.ComponentTests.Scenarios.DistributedLockResilience;

public class DistributedLockResilienceTests(App app) : AppComponentTest(app)
{
    private const int MaxRetryAttempts = 3;
    
    // The IDistributedLockProvider is decorated with TestDistributedLockProvider in WorkflowServer.ConfigureTestServices
    // This cast is safe because the decorator pattern ensures TestDistributedLockProvider wraps the actual provider
    private TestDistributedLockProvider MockProvider => (TestDistributedLockProvider)Scope.ServiceProvider.GetRequiredService<IDistributedLockProvider>();
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

    [Theory]
    [InlineData(1, 2, false)] // Single failure, succeeds on retry
    [InlineData(2, 3, false)] // Two failures, succeeds on third attempt
    [InlineData(4, 4, true)]  // Four failures, exhausts retries (MaxRetryAttempts = 3)
    public async Task RunInstanceAsync_TransientLockFailures_RetriesCorrectly(int failureCount, int expectedAttemptCount, bool shouldThrow)
    {
        // Arrange
        var workflowRuntime = Scope.ServiceProvider.GetRequiredService<IWorkflowRuntime>();
        var workflowClient = await workflowRuntime.CreateClientAsync();
        
        var createRequest = new CreateAndRunWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(SimpleWorkflow.DefinitionId, VersionOptions.Latest)
        };

        // Reset and configure failures right before the operation to minimize interference from background workers
        MockProvider.Reset();
        MockProvider.FailAcquisitionTimes(failureCount);

        // Act & Assert
        if (shouldThrow)
        {
            // Should throw after exhausting retries
            await Assert.ThrowsAsync<TimeoutException>(async () => 
                await workflowClient.CreateAndRunInstanceAsync(createRequest));
        }
        else
        {
            // Should succeed after retries
            var response = await workflowClient.CreateAndRunInstanceAsync(createRequest);
            Assert.NotNull(response);
            Assert.NotNull(response.WorkflowInstanceId);
        }

        // Verify retries occurred (allow for some background noise, but verify minimum attempts)
        Assert.True(MockProvider.AcquisitionAttemptCount >= expectedAttemptCount, 
            $"Expected at least {expectedAttemptCount} acquisition attempts, but got {MockProvider.AcquisitionAttemptCount}");
    }

    [Fact]
    public async Task RunInstanceAsync_TransientLockFailureOnSecondRun_RetriesCorrectly()
    {
        // Arrange
        var workflowRuntime = Scope.ServiceProvider.GetRequiredService<IWorkflowRuntime>();
        
        // First run - reset and don't inject failures
        MockProvider.Reset();
        var workflowClient = await workflowRuntime.CreateClientAsync();
        
        var createRequest = new CreateAndRunWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(SimpleWorkflow.DefinitionId, VersionOptions.Latest)
        };
        
        var firstResponse = await workflowClient.CreateAndRunInstanceAsync(createRequest);
        Assert.NotNull(firstResponse.WorkflowInstanceId);
        
        // Verify first run succeeded (at least 1 attempt, possibly more from background workers)
        Assert.True(MockProvider.AcquisitionAttemptCount >= 1);
        
        // Setup second workflow with failure - reset right before to minimize background noise
        MockProvider.Reset();
        MockProvider.FailAcquisitionTimes(2); // Fail twice, succeed on third
        
        var secondClient = await workflowRuntime.CreateClientAsync();
        var secondResponse = await secondClient.CreateAndRunInstanceAsync(createRequest);
        
        // Act & Assert - Second run should succeed after retries
        Assert.NotNull(secondResponse);
        Assert.NotNull(secondResponse.WorkflowInstanceId);
        Assert.NotEqual(firstResponse.WorkflowInstanceId, secondResponse.WorkflowInstanceId);
        
        // Verify retries occurred (at least 3 attempts: 2 failures + 1 success, possibly more from background)
        Assert.True(MockProvider.AcquisitionAttemptCount >= 3,
            $"Expected at least 3 acquisition attempts, but got {MockProvider.AcquisitionAttemptCount}");
    }

    [Fact]
    public async Task RunInstanceAsync_TransientReleaseFailure_ShouldLogButNotThrow()
    {
        // Arrange
        MockProvider.Reset();
        MockProvider.FailReleaseOnce();
        
        var workflowRuntime = Scope.ServiceProvider.GetRequiredService<IWorkflowRuntime>();
        var workflowClient = await workflowRuntime.CreateClientAsync();
        
        var createRequest = new CreateAndRunWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(SimpleWorkflow.DefinitionId, VersionOptions.Latest)
        };

        // Act - Release failure should be caught and logged, not thrown
        var response = await workflowClient.CreateAndRunInstanceAsync(createRequest);
        
        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.WorkflowInstanceId);
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
