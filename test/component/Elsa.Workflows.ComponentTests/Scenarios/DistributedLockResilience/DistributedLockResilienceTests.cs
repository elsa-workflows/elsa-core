using Elsa.Common.DistributedHosting;
using Elsa.Common.Models;
using Elsa.Resilience;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.ComponentTests.Scenarios.DistributedLockResilience.Mocks;
using Elsa.Workflows.ComponentTests.Scenarios.DistributedLockResilience.Workflows;
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

    [Theory]
    [InlineData(1, false)] // Single failure, succeeds on retry (2 attempts)
    [InlineData(2, false)] // Two failures, succeeds on third attempt (3 attempts)
    [InlineData(4, true)]  // Four failures, exhausts retries (MaxRetryAttempts = 3, so 4 attempts total)
    public async Task RunInstanceAsync_TransientLockFailures_RetriesCorrectly(int failureCount, bool shouldThrow)
    {
        // Arrange
        var workflowClient = await CreateWorkflowClientAsync();
        var workflowInstanceId = workflowClient.WorkflowInstanceId;

        // Reset and configure failures for this specific workflow instance's lock
        MockProvider.Reset();
        MockProvider.FailAcquisitionTimesForLock($"workflow-instance:{workflowInstanceId}", failureCount);
        var attemptCountBefore = MockProvider.AcquisitionAttemptCount;

        // Now run the instance with the configured lock failures
        var runRequest = new RunWorkflowInstanceRequest();

        // Act & Assert
        if (shouldThrow)
        {
            await Assert.ThrowsAsync<TimeoutException>(async () =>
                await workflowClient.RunInstanceAsync(runRequest));
        }
        else
        {
            var response = await workflowClient.RunInstanceAsync(runRequest);
            Assert.NotNull(response);
        }

        // Verify retries occurred - check the delta from before the operation to account for background noise
        var expectedAttempts = failureCount + 1; // failures + 1 success (or final failure for shouldThrow case)
        AssertMinimumAttempts(MockProvider.AcquisitionAttemptCount - attemptCountBefore, expectedAttempts, "acquisition");
    }

    [Fact]
    public async Task RunInstanceAsync_TransientReleaseFailure_ShouldLogButNotThrow()
    {
        // Arrange
        MockProvider.Reset();
        var workflowClient = await CreateWorkflowClientAsync(createInstance: false);

        // Configure failure after client creation to minimize background interference
        MockProvider.FailReleaseOnce();
        var releaseCountBefore = MockProvider.ReleaseAttemptCount;

        // Act - Release failure should be caught and logged, not thrown
        var response = await workflowClient.CreateAndRunInstanceAsync(CreateAndRunRequest());

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.WorkflowInstanceId);
        AssertMinimumAttempts(MockProvider.ReleaseAttemptCount - releaseCountBefore, 1, "release");
    }

    private async Task<IDistributedSynchronizationHandle?> AcquireLockWithRetryAsync(string lockName) =>
        await RetryPipeline.ExecuteAsync(async ct =>
            await MockProvider.AcquireLockAsync(lockName, LockOptions.LockAcquisitionTimeout, ct),
            CancellationToken.None);

    /// <summary>
    /// Creates a workflow client with an optional workflow instance already created.
    /// </summary>
    private async Task<IWorkflowClient> CreateWorkflowClientAsync(bool createInstance = true)
    {
        var workflowRuntime = Scope.ServiceProvider.GetRequiredService<IWorkflowRuntime>();
        var workflowClient = await workflowRuntime.CreateClientAsync();

        if (createInstance)
        {
            var createRequest = new CreateWorkflowInstanceRequest
            {
                WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(SimpleWorkflow.DefinitionId, VersionOptions.Latest)
            };
            await workflowClient.CreateInstanceAsync(createRequest);
        }

        return workflowClient;
    }

    private static CreateAndRunWorkflowInstanceRequest CreateAndRunRequest() =>
        new()
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(SimpleWorkflow.DefinitionId, VersionOptions.Latest)
        };

    private static void AssertMinimumAttempts(int actualAttempts, int expectedAttempts, string attemptType) =>
        Assert.True(actualAttempts >= expectedAttempts,
            $"Expected at least {expectedAttempts} {attemptType} attempts, but got {actualAttempts}");

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
