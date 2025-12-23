using Elsa.Common.DistributedHosting;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.State;
using Medallion.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace Elsa.Workflows.Runtime.Distributed;

public class DistributedWorkflowClient(
    string workflowInstanceId,
    IDistributedLockProvider distributedLockProvider,
    IOptions<DistributedLockingOptions> distributedLockingOptions,
    IServiceProvider serviceProvider,
    ILogger<DistributedWorkflowClient> logger)
    : IWorkflowClient
{
    private readonly LocalWorkflowClient _localWorkflowClient = ActivatorUtilities.CreateInstance<LocalWorkflowClient>(serviceProvider, workflowInstanceId);

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

    private async Task<R> WithLockAsync<R>(Func<Task<R>> func)
    {
        var lockKey = $"workflow-instance:{WorkflowInstanceId}";
        var lockTimeout = distributedLockingOptions.Value.LockAcquisitionTimeout;

        // Create a retry pipeline for transient connection failures
        var retryPipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(500),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder().Handle<Exception>(ex => IsTransientException(ex)),
                OnRetry = args =>
                {
                    logger.LogWarning(args.Outcome.Exception, "Transient error acquiring lock for workflow instance {WorkflowInstanceId}. Attempt {AttemptNumber} of {MaxAttempts}.", WorkflowInstanceId, args.AttemptNumber + 1, 3);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();

        IDistributedSynchronizationHandle? lockHandle = null;

        try
        {
            lockHandle = await retryPipeline.ExecuteAsync(async ct =>
                await distributedLockProvider.AcquireLockAsync(lockKey, lockTimeout, ct),
                CancellationToken.None);
            return await func();
        }
        finally
        {
            if (lockHandle != null)
            {
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
        }
    }

    private static bool IsTransientException(Exception ex)
    {
        // Check for common transient database connection errors
        return ex switch
        {
            // Network/connection errors
            System.Net.Sockets.SocketException => true,
            System.IO.IOException => true,
            TimeoutException => true,

            // Check nested exceptions for Npgsql
            _ when ex.InnerException is System.Net.Sockets.SocketException => true,
            _ when ex.InnerException is System.IO.IOException => true,
            _ when ex.InnerException is TimeoutException => true,

            // Npgsql specific transient errors
            _ when ex.GetType().FullName == "Npgsql.NpgsqlException" &&
                   (ex.Message.Contains("Connection refused") ||
                    ex.Message.Contains("Failed to connect") ||
                    ex.Message.Contains("timed out") ||
                    ex.Message.Contains("Connection is not open")) => true,

            _ => false
        };
    }
}