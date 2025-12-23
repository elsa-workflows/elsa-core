using Elsa.Common.DistributedHosting;
using Elsa.Resilience;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.State;
using Medallion.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace Elsa.Workflows.Runtime.Distributed;

public class DistributedWorkflowClient(
    string workflowInstanceId,
    IDistributedLockProvider distributedLockProvider,
    ITransientExceptionDetector transientExceptionDetector,
    IOptions<DistributedLockingOptions> distributedLockingOptions,
    IServiceProvider serviceProvider,
    ILogger<DistributedWorkflowClient> logger)
    : IWorkflowClient
{
    private readonly LocalWorkflowClient _localWorkflowClient = ActivatorUtilities.CreateInstance<LocalWorkflowClient>(serviceProvider, workflowInstanceId);
    private readonly Lazy<ResiliencePipeline> _retryPipeline = new(() => CreateRetryPipeline(transientExceptionDetector, logger, workflowInstanceId));

    public string WorkflowInstanceId => workflowInstanceId;

    public async Task<CreateWorkflowInstanceResponse> CreateInstanceAsync(CreateWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
    {
        return await _localWorkflowClient.CreateInstanceAsync(request, cancellationToken);
    }

    public async Task<RunWorkflowInstanceResponse> RunInstanceAsync(RunWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
    {
        var result = await WithLockAsync(async () => await _localWorkflowClient.RunInstanceAsync(request, cancellationToken));
        return result;
    }

    public async Task<RunWorkflowInstanceResponse> CreateAndRunInstanceAsync(CreateAndRunWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
    {
        var createRequest = new CreateWorkflowInstanceRequest
        {
            Properties = request.Properties,
            CorrelationId = request.CorrelationId,
            Name = request.Name,
            Input = request.Input,
            WorkflowDefinitionHandle = request.WorkflowDefinitionHandle,
            ParentId = request.ParentId
        };
        var workflowInstance = await _localWorkflowClient.CreateInstanceInternalAsync(createRequest, cancellationToken);
        
        // We need to lock newly created workflow instances too, because it might dispatch child workflows that attempt to resume the parent workflow.
        // For example, when using a DispatchWorkflow activity configured to wait for the dispatched workflow to complete.
        return await WithLockAsync(async () => await _localWorkflowClient.RunInstanceAsync(workflowInstance, new()
        {
            Input = request.Input,
            Variables = request.Variables,
            Properties = request.Properties,
            TriggerActivityId = request.TriggerActivityId,
            ActivityHandle = request.ActivityHandle,
            IncludeWorkflowOutput = request.IncludeWorkflowOutput
        }, cancellationToken));
    }

    public async Task CancelAsync(CancellationToken cancellationToken = default)
    {
        await _localWorkflowClient.CancelAsync(cancellationToken);
    }

    public async Task<WorkflowState> ExportStateAsync(CancellationToken cancellationToken = default)
    {
        return await _localWorkflowClient.ExportStateAsync(cancellationToken);
    }

    public async Task ImportStateAsync(WorkflowState workflowState, CancellationToken cancellationToken = default)
    {
        await _localWorkflowClient.ImportStateAsync(workflowState, cancellationToken);
    }

    public async Task<bool> InstanceExistsAsync(CancellationToken cancellationToken = default)
    {
        return await _localWorkflowClient.InstanceExistsAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(CancellationToken cancellationToken = default)
    {
        // Use the same distributed lock as for execution to prevent concurrent DB writes
        return await WithLockAsync(async () => await _localWorkflowClient.DeleteAsync(cancellationToken));
    }

    private async Task<TReturn> WithLockAsync<TReturn>(Func<Task<TReturn>> func)
    {
        var lockKey = $"workflow-instance:{WorkflowInstanceId}";
        var lockHandle = await AcquireLockWithRetryAsync(lockKey);

        try
        {
            return await func();
        }
        finally
        {
            await ReleaseLockAsync(lockHandle);
        }
    }

    private async Task<IDistributedSynchronizationHandle> AcquireLockWithRetryAsync(string lockKey)
    {
        var lockTimeout = distributedLockingOptions.Value.LockAcquisitionTimeout;

        return await _retryPipeline.Value.ExecuteAsync(async ct =>
            await distributedLockProvider.AcquireLockAsync(lockKey, lockTimeout, ct),
            CancellationToken.None);
    }

    private async Task ReleaseLockAsync(IDistributedSynchronizationHandle? lockHandle)
    {
        if (lockHandle == null)
            return;

        try
        {
            await lockHandle.DisposeAsync();
        }
        catch (Exception ex)
        {
            // Log but don't throw - the work is already done, and the lock
            // will be automatically released when the connection dies
            logger.LogWarning(ex, "Failed to release distributed lock for workflow instance {WorkflowInstanceId}. The lock will be automatically released by the database.", WorkflowInstanceId);
        }
    }

    private static ResiliencePipeline CreateRetryPipeline(
        ITransientExceptionDetector transientExceptionDetector,
        ILogger<DistributedWorkflowClient> logger,
        string workflowInstanceId)
    {
        const int maxRetryAttempts = 3;

        return new ResiliencePipelineBuilder()
            .AddRetry(new()
            {
                MaxRetryAttempts = maxRetryAttempts,
                Delay = TimeSpan.FromMilliseconds(500),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder().Handle<Exception>(transientExceptionDetector.IsTransient),
                OnRetry = args =>
                {
                    logger.LogWarning(args.Outcome.Exception, "Transient error acquiring lock for workflow instance {WorkflowInstanceId}. Attempt {AttemptNumber} of {MaxAttempts}.", workflowInstanceId, args.AttemptNumber + 1, maxRetryAttempts);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }
}