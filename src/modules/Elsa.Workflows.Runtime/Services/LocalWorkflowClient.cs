using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// Represents a client for executing and managing local workflows.
/// </summary>
public class LocalWorkflowClient(
    string workflowInstanceId,
    IWorkflowInstanceManager workflowInstanceManager,
    IWorkflowDefinitionService workflowDefinitionService,
    IWorkflowHostFactory workflowHostFactory) : IWorkflowClient
{
    /// <inheritdoc />
    public string WorkflowInstanceId => workflowInstanceId;

    /// <inheritdoc />
    public async Task<CreateWorkflowInstanceResponse> CreateInstanceAsync(CreateWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
    {
        var workflowDefinitionVersionId = request.WorkflowDefinitionHandle;
        var workflow = await workflowDefinitionService.FindWorkflowAsync(workflowDefinitionVersionId, cancellationToken);
        if (workflow == null) throw new InvalidOperationException($"Workflow with version ID {workflowDefinitionVersionId} not found.");

        var options = new WorkflowInstanceOptions
        {
            WorkflowInstanceId = WorkflowInstanceId,
            CorrelationId = request.CorrelationId,
            ParentWorkflowInstanceId = request.ParentId,
            Input = request.Input,
            Properties = request.Properties
        };

        await workflowInstanceManager.CreateWorkflowInstanceAsync(workflow, options, cancellationToken);
        return new CreateWorkflowInstanceResponse();
    }

    /// <inheritdoc />
    public async Task<RunWorkflowInstanceResponse> RunAsync(RunWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
    {
        var workflowHost = await CreateWorkflowHostAsync(cancellationToken);
        var startWorkflowRequest = new RunWorkflowParams
        {
            Input = request.Input,
            Properties = request.Properties,
            BookmarkId = request.BookmarkId,
            TriggerActivityId = request.TriggerActivityId,
            ActivityHandle = request.ActivityHandle
        };
        await workflowHost.RunWorkflowAsync(startWorkflowRequest, cancellationToken);
        return new RunWorkflowInstanceResponse
        {
            WorkflowInstanceId = WorkflowInstanceId,
            Status = workflowHost.WorkflowState.Status,
            SubStatus = workflowHost.WorkflowState.SubStatus,
            Incidents = workflowHost.WorkflowState.Incidents
        };
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
        var workflowInstance = await workflowInstanceManager.FindByIdAsync(WorkflowInstanceId, cancellationToken);
        if (workflowInstance == null) throw new InvalidOperationException($"Workflow instance {WorkflowInstanceId} not found. Please call CreateInstanceAsync first.");

        return await CreateWorkflowHostAsync(workflowInstance, cancellationToken);
    }

    private async Task<IWorkflowHost> CreateWorkflowHostAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken)
    {
        var workflowDefinitionVersionId = workflowInstance.DefinitionVersionId;
        var workflow = await workflowDefinitionService.FindWorkflowAsync(workflowDefinitionVersionId, cancellationToken);
        if (workflow == null) throw new InvalidOperationException($"Workflow {workflowDefinitionVersionId} not found.");
        return await workflowHostFactory.CreateAsync(workflow, workflowInstance.WorkflowState, cancellationToken);
    }
}