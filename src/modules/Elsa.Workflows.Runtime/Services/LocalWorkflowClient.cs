using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Exceptions;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.Runtime.Exceptions;
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
            Name = request.Name,
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
            Name = request.Name,
            Input = request.Input,
            WorkflowDefinitionHandle = request.WorkflowDefinitionHandle,
            ParentId = request.ParentId
        };
        var workflowInstance = await CreateInstanceInternalAsync(createRequest, cancellationToken);
        return await RunInstanceAsync(workflowInstance, new()
        {
            Input = request.Input,
            Variables = request.Variables,
            Properties = request.Properties,
            TriggerActivityId = request.TriggerActivityId,
            ActivityHandle = request.ActivityHandle,
            IncludeWorkflowOutput = request.IncludeWorkflowOutput,
            SchedulingActivityExecutionId = request.SchedulingActivityExecutionId,
            SchedulingWorkflowInstanceId = request.SchedulingWorkflowInstanceId
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task CancelAsync(CancellationToken cancellationToken = default)
    {
        var workflowInstance = await GetWorkflowInstanceAsync(cancellationToken);
        await CancelAsync(workflowInstance, cancellationToken);
    }

    private async Task CancelAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken)
    {
        if (workflowInstance.Status != WorkflowStatus.Running) return;
        var workflowGraph = await TryGetWorkflowGraphAsync(workflowInstance, cancellationToken);
        
        if (workflowGraph == null) 
            return;
        
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

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(CancellationToken cancellationToken = default)
    {
        // Load the workflow instance (single DB call)
        var workflowInstance = await TryGetWorkflowInstanceAsync(cancellationToken);
        if (workflowInstance == null)
            return false;

        await CancelAsync(workflowInstance, cancellationToken);
        
        // Delete the workflow instance
        var filter = new WorkflowInstanceFilter { Id = workflowInstanceId };
        await workflowInstanceManager.DeleteAsync(filter, cancellationToken);
        return true;
    }

    public async Task<RunWorkflowInstanceResponse> RunInstanceAsync(WorkflowInstance workflowInstance, RunWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
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
            Variables = request.Variables,
            Properties = request.Properties,
            BookmarkId = request.BookmarkId,
            TriggerActivityId = request.TriggerActivityId,
            ActivityHandle = request.ActivityHandle,
            SchedulingActivityExecutionId = request.SchedulingActivityExecutionId,
            SchedulingWorkflowInstanceId = request.SchedulingWorkflowInstanceId
        };

        var workflowGraph = await GetWorkflowGraphAsync(workflowInstance, cancellationToken);
        var workflowResult = await workflowRunner.RunAsync(workflowGraph, workflowState, runWorkflowOptions, cancellationToken);

        workflowState = workflowResult.WorkflowState;

        return new()
        {
            WorkflowInstanceId = WorkflowInstanceId,
            Status = workflowState.Status,
            SubStatus = workflowState.SubStatus,
            Incidents = workflowState.Incidents,
            Output = request.IncludeWorkflowOutput ? new Dictionary<string, object>(workflowState.Output) : null,
            Bookmarks = workflowState.Bookmarks
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
            Name = request.Name,
            ParentWorkflowInstanceId = request.ParentId,
            Input = request.Input,
            Properties = request.Properties
        };

        return workflowInstanceManager.CreateWorkflowInstance(workflowGraph.Workflow, options);
    }

    private async Task<WorkflowInstance> GetWorkflowInstanceAsync(CancellationToken cancellationToken)
    {
        var workflowInstance = await TryGetWorkflowInstanceAsync(cancellationToken);
        if (workflowInstance == null) throw new WorkflowInstanceNotFoundException("Workflow instance not found.", WorkflowInstanceId);
        return workflowInstance;
    }

    private Task<WorkflowInstance?> TryGetWorkflowInstanceAsync(CancellationToken cancellationToken)
    {
        return workflowInstanceManager.FindByIdAsync(WorkflowInstanceId, cancellationToken);
    }

    private async Task<WorkflowGraph> GetWorkflowGraphAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken)
    {
        var handle = WorkflowDefinitionHandle.ByDefinitionVersionId(workflowInstance.DefinitionVersionId);
        return await GetWorkflowGraphAsync(handle, cancellationToken);
    }

    private async Task<WorkflowGraph> GetWorkflowGraphAsync(WorkflowDefinitionHandle definitionHandle, CancellationToken cancellationToken)
    {
        var result = await workflowDefinitionService.TryFindWorkflowGraphAsync(definitionHandle, cancellationToken);
        if (!result.WorkflowDefinitionExists) throw new WorkflowDefinitionNotFoundException("Workflow definition not found.", definitionHandle);
        if (!result.WorkflowGraphExists) throw new WorkflowMaterializerNotFoundException(result.WorkflowDefinition!.MaterializerName);
        return result.WorkflowGraph!;
    }
    
    private async Task<WorkflowGraph?> TryGetWorkflowGraphAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken)
    {
        var handle = WorkflowDefinitionHandle.ByDefinitionVersionId(workflowInstance.DefinitionVersionId);
        return await TryGetWorkflowGraphAsync(handle, cancellationToken);
    }
    
    private async Task<WorkflowGraph?> TryGetWorkflowGraphAsync(WorkflowDefinitionHandle definitionHandle, CancellationToken cancellationToken)
    {
        var result = await workflowDefinitionService.TryFindWorkflowGraphAsync(definitionHandle, cancellationToken);
        if (!result.WorkflowDefinitionExists)
        {
            logger.LogWarning("Workflow definition not found: {WorkflowDefinitionId}", definitionHandle.DefinitionId);
            return null;
        }

        if (!result.WorkflowGraphExists)
        {
            logger.LogWarning("Workflow materializer not found: {WorkflowDefinitionId}", definitionHandle.DefinitionId);
            return null;
        }
        
        return result.WorkflowGraph!;
    }
}