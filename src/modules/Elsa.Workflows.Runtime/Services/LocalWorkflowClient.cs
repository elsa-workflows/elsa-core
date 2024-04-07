using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Parameters;
using Elsa.Workflows.Runtime.Results;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// Represents a client for executing and managing local workflows.
/// </summary>
public class LocalWorkflowClient(
    IWorkflowInstanceManager workflowInstanceManager,
    IWorkflowDefinitionService workflowDefinitionService,
    IWorkflowHostFactory workflowHostFactory) : IWorkflowClient
{
    /// <inheritdoc />
    public string WorkflowDefinitionVersionId { get; set; } = default!;

    /// <inheritdoc />
    public string WorkflowInstanceId { get; set; } = default!;

    /// <inheritdoc />
    public async Task<ExecuteWorkflowResult> ExecuteAndWaitAsync(IExecuteWorkflowRequest? request = default, CancellationToken cancellationToken = default)
    {
        var workflowHost = await CreateWorkflowHostAsync(cancellationToken);
        return await workflowHost.ExecuteWorkflowAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public Task ExecuteAndForgetAsync(IExecuteWorkflowRequest? request = default, CancellationToken cancellationToken = default)
    {
        _ = ExecuteAndWaitAsync(request, cancellationToken);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task CancelAsync(CancellationToken cancellationToken = default)
    {
        var workflowHost = await CreateWorkflowHostAsync(cancellationToken);
        await workflowHost.CancelWorkflowAsync(cancellationToken);
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