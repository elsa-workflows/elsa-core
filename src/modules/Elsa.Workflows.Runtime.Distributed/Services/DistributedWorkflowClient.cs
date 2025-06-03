using Elsa.Common.DistributedHosting;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.State;
using Medallion.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime.Distributed;

public class DistributedWorkflowClient(
    string workflowInstanceId,
    IDistributedLockProvider distributedLockProvider,
    IOptions<DistributedLockingOptions> distributedLockingOptions,
    IServiceProvider serviceProvider)
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

    private async Task<R> WithLockAsync<R>(Func<Task<R>> func)
    {
        var lockKey = $"workflow-instance:{WorkflowInstanceId}";
        var lockTimeout = distributedLockingOptions.Value.LockAcquisitionTimeout;
        await using var @lock = await distributedLockProvider.AcquireLockAsync(lockKey, lockTimeout);
        var result = await func();
        return result;
    }
}