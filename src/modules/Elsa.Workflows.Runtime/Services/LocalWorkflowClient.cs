using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.State;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime.Services;

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
        var workflowGraph = await workflowDefinitionService.FindWorkflowGraphAsync(workflowDefinitionHandle, cancellationToken);
        if (workflowGraph == null) throw new InvalidOperationException($"Workflow with version ID {workflowDefinitionHandle} not found.");

        var options = new WorkflowInstanceOptions
        {
            WorkflowInstanceId = WorkflowInstanceId,
            CorrelationId = request.CorrelationId,
            ParentWorkflowInstanceId = request.ParentId,
            Input = request.Input,
            Properties = request.Properties
        };

        await workflowInstanceManager.CreateWorkflowInstanceAsync(workflowGraph.Workflow, options, cancellationToken);
        return new CreateWorkflowInstanceResponse();
    }

    /// <inheritdoc />
    public async Task<RunWorkflowInstanceResponse> RunInstanceAsync(RunWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
    {
        var workflowInstance = await GetWorkflowInstanceAsync(cancellationToken);
        var workflowState = workflowInstance.WorkflowState;

        if (workflowInstance.Status != WorkflowStatus.Running)
        {
            logger.LogWarning("Attempt to resume workflow {WorkflowInstanceId} that is not in the Running state. The actual state is {ActualWorkflowStatus}", workflowState.Id, workflowState.Status);
            return new RunWorkflowInstanceResponse
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

        var workflowGraph = await GetWorkflowGraphAsync(cancellationToken);
        var workflowResult = await workflowRunner.RunAsync(workflowGraph, workflowState, runWorkflowOptions, cancellationToken);

        workflowState = workflowResult.WorkflowState;
        await workflowInstanceManager.SaveAsync(workflowState, cancellationToken);

        return new RunWorkflowInstanceResponse
        {
            WorkflowInstanceId = WorkflowInstanceId,
            Status = workflowState.Status,
            SubStatus = workflowState.SubStatus,
            Incidents = workflowState.Incidents
        };
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
        await CreateInstanceAsync(createRequest, cancellationToken);
        return await RunInstanceAsync(new RunWorkflowInstanceRequest
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
        var workflowGraph = await GetWorkflowGraphAsync(cancellationToken);
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

    private async Task<WorkflowInstance> GetWorkflowInstanceAsync(CancellationToken cancellationToken)
    {
        var workflowInstance = await workflowInstanceManager.FindByIdAsync(WorkflowInstanceId, cancellationToken);
        if (workflowInstance == null) throw new InvalidOperationException($"Workflow instance {WorkflowInstanceId} not found.");
        return workflowInstance;
    }

    private async Task<WorkflowGraph> GetWorkflowGraphAsync(CancellationToken cancellationToken)
    {
        var workflowInstance = await GetWorkflowInstanceAsync(cancellationToken);
        var workflowGraph = await workflowDefinitionService.FindWorkflowGraphAsync(workflowInstance.DefinitionVersionId, cancellationToken);
        if (workflowGraph == null) throw new InvalidOperationException($"Workflow graph with version ID {workflowInstance.DefinitionVersionId} not found.");
        return workflowGraph;
    }
}