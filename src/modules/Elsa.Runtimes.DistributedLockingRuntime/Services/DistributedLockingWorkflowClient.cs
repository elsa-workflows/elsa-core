using Elsa.Mediator.Contracts;
using Elsa.Runtimes.DistributedLockingRuntime.Commands;
using Elsa.Workflows;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Parameters;
using Elsa.Workflows.Runtime.Results;
using Elsa.Workflows.State;
using Medallion.Threading;

namespace Elsa.Runtimes.DistributedLockingRuntime.Services;

/// <summary>
/// Represents a client that can interact with a workflow instance.
/// </summary>
public class DistributedLockingWorkflowClient(
    IWorkflowInstanceManager workflowInstanceManager,
    IWorkflowDefinitionService workflowDefinitionService,
    IWorkflowHostFactory workflowHostFactory,
    IWorkflowExecutionContextStore workflowExecutionContextStore,
    IWorkflowCanceler workflowCanceler,
    IDistributedLockProvider distributedLockProvider,
    ICommandSender commandSender) : IWorkflowClient
{
    /// <inheritdoc />
    public string WorkflowInstanceId { get; set; } = default!;

    /// <inheritdoc />
    public async Task<ExecuteWorkflowResult> ExecuteAndWaitAsync(IExecuteWorkflowRequest? @params = null, CancellationToken cancellationToken = default)
    {
        var workflowHost = await CreateWorkflowHostAsync(cancellationToken);
        return await workflowHost.ExecuteWorkflowAsync(@params, cancellationToken);
    }

    /// <inheritdoc />
    public async Task ExecuteAndForgetAsync(IExecuteWorkflowRequest? @params = default, CancellationToken cancellationToken = default)
    {
        var command = new ExecuteWorkflowCommand(@params);
        await commandSender.SendAsync(command, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CancellationResult> CancelAsync(CancellationToken cancellationToken = default)
    {
        var workflowExecutionContext = await workflowExecutionContextStore.FindAsync(WorkflowInstanceId);
        var isRunningLocally = workflowExecutionContext != null;
        var cancellationLockKey = $"Cancelling:{WorkflowInstanceId}";

        // If the cancellation is already in progress, return immediately.
        await using var cancellationLock =
            isRunningLocally
                ? await distributedLockProvider.AcquireLockAsync(cancellationLockKey, cancellationToken: cancellationToken)
                : await distributedLockProvider.TryAcquireLockAsync(cancellationLockKey, cancellationToken: cancellationToken);

        if (cancellationLock == null)
            return new CancellationResult(false, CancellationFailureReason.CancellationAlreadyInProgress);

        if (!isRunningLocally)
        {
            // The execution context is not running on this instance, but we did acquire the cancellation lock. Which means no other instance is currently cancelling the workflow.
            var workflowInstance = await FindWorkflowInstanceAsync(cancellationToken);
            if (workflowInstance is null)
                return new CancellationResult(false, CancellationFailureReason.NotFound);
            if (workflowInstance.Status == WorkflowStatus.Finished)
                return new CancellationResult(false, CancellationFailureReason.AlreadyCancelled);
            
            var workflowHost = await CreateWorkflowHostAsync(workflowInstance, cancellationToken);
            await workflowHost.CancelWorkflowAsync(cancellationToken);

            return new CancellationResult(true);
        }

        // The execution context is running on this instance. Cancel the workflow.
        await workflowCanceler.CancelWorkflowAsync(workflowExecutionContext!, cancellationToken);
        return new CancellationResult(true);
    }

    /// <inheritdoc />
    public async Task<WorkflowState> ExportStateAsync(CancellationToken cancellationToken = default)
    {
        var workflowHost = await CreateWorkflowHostAsync(cancellationToken);
        return workflowHost.WorkflowState;
    }

    /// <inheritdoc />
    public async Task ImportStateAsync(WorkflowState workflowState, CancellationToken cancellationToken = default)
    {
        var workflowHost = await CreateWorkflowHostAsync(cancellationToken);
        workflowHost.WorkflowState = workflowState;
        await workflowHost.PersistStateAsync(cancellationToken);
    }

    private async Task<IWorkflowHost> CreateWorkflowHostAsync(CancellationToken cancellationToken)
    {
        var workflowInstance = await FindWorkflowInstanceAsync(cancellationToken);

        if (workflowInstance == null)
            throw new InvalidOperationException($"Workflow instance {WorkflowInstanceId} not found.");

        return await CreateWorkflowHostAsync(workflowInstance, cancellationToken);
    }
    
    private async Task<IWorkflowHost> CreateWorkflowHostAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken)
    {
        var workflow = await workflowDefinitionService.FindWorkflowAsync(workflowInstance.DefinitionVersionId, cancellationToken);

        if (workflow == null)
            throw new InvalidOperationException($"Workflow {workflowInstance.DefinitionId} not found.");

        return await workflowHostFactory.CreateAsync(workflow, workflowInstance.WorkflowState, cancellationToken);
    }
    
    private async Task<WorkflowInstance?> FindWorkflowInstanceAsync(CancellationToken cancellationToken)
    {
        var filter = new WorkflowInstanceFilter
        {
            Id = WorkflowInstanceId
        };
        return await workflowInstanceManager.FindAsync(filter, cancellationToken);
    }
}