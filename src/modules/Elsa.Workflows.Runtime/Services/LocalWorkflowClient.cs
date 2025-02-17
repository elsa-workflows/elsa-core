using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.State;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Represents a client for executing and managing local workflows.
/// </summary>
public class LocalWorkflowClient(
    string workflowInstanceId,
    IWorkflowInstanceManager workflowInstanceManager,
    IWorkflowDefinitionService workflowDefinitionService,
    IWorkflowRunner workflowRunner,
    IWorkflowCanceler workflowCanceler,
    WorkflowStateMapper workflowStateMapper,
    ILogger<LocalWorkflowClient> logger) : IWorkflowClient
{
    /// <inheritdoc />
    public string WorkflowInstanceId => workflowInstanceId;

    /// <inheritdoc />
    public async Task<CreateWorkflowInstanceResponse> CreateInstanceAsync(CreateWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
    {
        var workflowDefinitionHandle = request.WorkflowDefinitionHandle;
        var workflowGraph = await GetWorkflowGraphAsync(workflowDefinitionHandle, cancellationToken);

        var options = new WorkflowInstanceOptions
        {
            WorkflowInstanceId = WorkflowInstanceId,
            CorrelationId = request.CorrelationId,
            ParentWorkflowInstanceId = request.ParentId,
            Input = request.Input,
            Properties = request.Properties
        };

        await workflowInstanceManager.CreateAndCommitWorkflowInstanceAsync(workflowGraph.Workflow, options, cancellationToken);
        return new();
    }

    /// <inheritdoc />
    public async Task<RunWorkflowInstanceResponse> RunInstanceAsync(RunWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
    {
        var workflowInstance = await GetWorkflowInstanceAsync(cancellationToken);
        return await RunInstanceAsync(workflowInstance, request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RunWorkflowInstanceResponse> CreateAndRunInstanceAsync(CreateAndRunWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
    {
        var createRequest = new CreateWorkflowInstanceRequest
        {
            Properties = request.Properties,
            CorrelationId = request.CorrelationId,
            Input = request.Input,
            WorkflowDefinitionHandle = request.WorkflowDefinitionHandle,
            ParentId = request.ParentId
        };
        var workflowInstance = await CreateInstanceInternalAsync(createRequest, cancellationToken);
        return await RunInstanceAsync(workflowInstance, new()
        {
            Input = request.Input,
            Properties = request.Properties,
            TriggerActivityId = request.TriggerActivityId,
            ActivityHandle = request.ActivityHandle
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task CancelAsync(CancellationToken cancellationToken = default)
    {
        var workflowInstance = await GetWorkflowInstanceAsync(cancellationToken);
        if (workflowInstance.Status != WorkflowStatus.Running) return;
        var workflowGraph = await GetWorkflowGraphAsync(workflowInstance, cancellationToken);
        var workflowState = await workflowCanceler.CancelWorkflowAsync(workflowGraph, workflowInstance.WorkflowState, cancellationToken);
        await workflowInstanceManager.SaveAsync(workflowState, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowState> ExportStateAsync(CancellationToken cancellationToken = default)
    {
        var workflowInstance = await GetWorkflowInstanceAsync(cancellationToken);
        return workflowInstance.WorkflowState;
    }

    /// <inheritdoc />
    public async Task ImportStateAsync(WorkflowState workflowState, CancellationToken cancellationToken = default)
    {
        var workflowInstance = workflowStateMapper.Map(workflowState)!;
        await workflowInstanceManager.SaveAsync(workflowInstance, cancellationToken);
    }

    public Task<bool> InstanceExistsAsync(CancellationToken cancellationToken = default)
    {
        return workflowInstanceManager.ExistsAsync(workflowInstanceId, cancellationToken);
    }

    private async Task<RunWorkflowInstanceResponse> RunInstanceAsync(WorkflowInstance workflowInstance, RunWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
    {
        var workflowState = workflowInstance.WorkflowState;

        if (workflowInstance.Status != WorkflowStatus.Running)
        {
            logger.LogWarning("Attempt to resume workflow {WorkflowInstanceId} that is not in the Running state. The actual state is {ActualWorkflowStatus}", workflowState.Id, workflowState.Status);
            return new()
            {
                WorkflowInstanceId = WorkflowInstanceId,
                Status = workflowInstance.Status,
                SubStatus = workflowInstance.SubStatus
            };
        }

        var runWorkflowOptions = new RunWorkflowOptions
        {
            Input = request.Input,
            Properties = request.Properties,
            BookmarkId = request.BookmarkId,
            TriggerActivityId = request.TriggerActivityId,
            ActivityHandle = request.ActivityHandle,
        };

        var workflowGraph = await GetWorkflowGraphAsync(workflowInstance, cancellationToken);
        var workflowResult = await workflowRunner.RunAsync(workflowGraph, workflowState, runWorkflowOptions, cancellationToken);

        workflowState = workflowResult.WorkflowState;

        return new()
        {
            WorkflowInstanceId = WorkflowInstanceId,
            Status = workflowState.Status,
            SubStatus = workflowState.SubStatus,
            Incidents = workflowState.Incidents
        };
    }
    
    public async Task<WorkflowInstance> CreateInstanceInternalAsync(CreateWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
    {
        var workflowDefinitionHandle = request.WorkflowDefinitionHandle;
        var workflowGraph = await GetWorkflowGraphAsync(workflowDefinitionHandle, cancellationToken);

        var options = new WorkflowInstanceOptions
        {
            WorkflowInstanceId = WorkflowInstanceId,
            CorrelationId = request.CorrelationId,
            ParentWorkflowInstanceId = request.ParentId,
            Input = request.Input,
            Properties = request.Properties
        };

        return workflowInstanceManager.CreateWorkflowInstance(workflowGraph.Workflow, options);
    }

    private async Task<WorkflowInstance> GetWorkflowInstanceAsync(CancellationToken cancellationToken)
    {
        var workflowInstance = await workflowInstanceManager.FindByIdAsync(WorkflowInstanceId, cancellationToken);
        if (workflowInstance == null) throw new InvalidOperationException($"Workflow instance {WorkflowInstanceId} not found.");
        return workflowInstance;
    }

    private async Task<WorkflowGraph> GetWorkflowGraphAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken)
    {
        var handle = WorkflowDefinitionHandle.ByDefinitionVersionId(workflowInstance.DefinitionVersionId);
        return await GetWorkflowGraphAsync(handle, cancellationToken);
    }

    private async Task<WorkflowGraph> GetWorkflowGraphAsync(WorkflowDefinitionHandle definitionHandle, CancellationToken cancellationToken)
    {
        var workflowGraph = await workflowDefinitionService.FindWorkflowGraphAsync(definitionHandle, cancellationToken);
        if (workflowGraph == null) throw new InvalidOperationException($"Workflow graph with handle {definitionHandle} not found.");
        return workflowGraph;
    }
}