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

    // Selective mock provider - only mocks specific locks, not all locks globally
    private SelectiveMockLockProvider SelectiveMockProvider => Scope.ServiceProvider.GetRequiredService<SelectiveMockLockProvider>();

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
        // Arrange - Mock this specific lock only
        var lockName = $"test-lock-{failureCount}";
        var mockProvider = SelectiveMockProvider.MockLock(lockName);
        mockProvider.Reset();
        mockProvider.FailAcquisitionTimes(failureCount);

        // Act & Assert
        if (shouldThrow)
        {
            await Assert.ThrowsAsync<TimeoutException>(async () => await AcquireLockWithRetryAsync(lockName, mockProvider));
        }
        else
        {
            await using var handle = await AcquireLockWithRetryAsync(lockName, mockProvider);
            Assert.NotNull(handle);
        }

        // Assert exact count - only this lock is mocked
        Assert.Equal(expectedAttemptCount, mockProvider.AcquisitionAttemptCount);
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

        // Configure failures for this specific workflow instance's lock only
        var lockPrefix = $"workflow-instance:{workflowInstanceId}";
        var mockProvider = SelectiveMockProvider.MockLock(lockPrefix);
        mockProvider.Reset();
        mockProvider.FailAcquisitionTimes(failureCount);

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

        // Assert exact count - only this specific workflow instance lock is mocked
        // When shouldThrow=true, all attempts fail: MaxRetryAttempts+1 (initial + retries)
        // When shouldThrow=false, we succeed after failures: failureCount+1 (failures + success)
        var expectedAttempts = shouldThrow ? MaxRetryAttempts + 1 : failureCount + 1;
        Assert.Equal(expectedAttempts, mockProvider.AcquisitionAttemptCount);
    }

    [Fact]
    public async Task RunInstanceAsync_TransientReleaseFailure_ShouldLogButNotThrow()
    {
        // Arrange
        var workflowClient = await CreateWorkflowClientAsync(createInstance: false);

        // Create and run to get the workflow instance ID, then configure release failure for that lock
        var request = CreateAndRunRequest();

        // Mock the workflow-instance lock prefix (all workflow instance locks)
        var mockProvider = SelectiveMockProvider.MockLock("workflow-instance:");
        mockProvider.Reset();
        mockProvider.FailReleaseOnce();

        // Act - Release failure should be caught and logged, not thrown
        var response = await workflowClient.CreateAndRunInstanceAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.WorkflowInstanceId);

        // Verify at least one release occurred
        Assert.True(mockProvider.ReleaseAttemptCount >= 1,
            $"Expected at least 1 release attempt, but got {mockProvider.ReleaseAttemptCount}");
    }

    private async Task<IDistributedSynchronizationHandle?> AcquireLockWithRetryAsync(string lockName, TestDistributedLockProvider mockProvider) =>
        await RetryPipeline.ExecuteAsync(async ct =>
            await mockProvider.CreateLock(lockName).AcquireAsync(LockOptions.LockAcquisitionTimeout, ct),
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
