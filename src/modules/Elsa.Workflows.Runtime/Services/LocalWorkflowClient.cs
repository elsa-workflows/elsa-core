using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Exceptions;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.Runtime.Notifications;
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
    IBookmarkStore bookmarkStore,
    INotificationSender notificationSender,
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
            IncludeWorkflowOutput = request.IncludeWorkflowOutput
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
    public async Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        var workflowInstance = await workflowInstanceManager.FindByIdAsync(WorkflowInstanceId, cancellationToken);
        
        // If the instance doesn't exist, nothing to delete
        if (workflowInstance == null)
            return;

        // Mark the workflow as deleted (soft delete)
        workflowInstance.Status = WorkflowStatus.Finished;
        workflowInstance.SubStatus = WorkflowSubStatus.Deleted;
        workflowInstance.FinishedAt = DateTimeOffset.UtcNow;
        
        // Update workflow state to reflect deletion
        workflowInstance.WorkflowState.Status = WorkflowStatus.Finished;
        workflowInstance.WorkflowState.SubStatus = WorkflowSubStatus.Deleted;
        
        // Save the marked instance
        await workflowInstanceManager.UpdateAsync(workflowInstance, cancellationToken);
        
        // Delete associated bookmarks
        var bookmarkFilter = new BookmarkFilter { WorkflowInstanceId = WorkflowInstanceId };
        var bookmarks = (await bookmarkStore.FindManyAsync(bookmarkFilter, cancellationToken)).ToList();
        
        if (bookmarks.Count > 0)
        {
            await notificationSender.SendAsync(new BookmarksDeleting(bookmarks), cancellationToken);
            await bookmarkStore.DeleteAsync(bookmarkFilter, cancellationToken);
            await notificationSender.SendAsync(new BookmarksDeleted(bookmarks), cancellationToken);
        }
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

    public async Task<RunWorkflowInstanceResponse> RunInstanceAsync(WorkflowInstance workflowInstance, RunWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
    {
        var workflowState = workflowInstance.WorkflowState;

        // Don't execute deleted workflows
        if (workflowInstance.SubStatus == WorkflowSubStatus.Deleted)
        {
            logger.LogWarning("Attempt to resume workflow {WorkflowInstanceId} that has been deleted", workflowState.Id);
            return new()
            {
                WorkflowInstanceId = WorkflowInstanceId,
                Status = workflowInstance.Status,
                SubStatus = workflowInstance.SubStatus
            };
        }

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
        var workflowInstance = await workflowInstanceManager.FindByIdAsync(WorkflowInstanceId, cancellationToken);
        if (workflowInstance == null) throw new WorkflowInstanceNotFoundException($"Workflow instance not found.", WorkflowInstanceId);
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
        if (workflowGraph == null) throw new WorkflowGraphNotFoundException($"Workflow graph not found.", definitionHandle);
        return workflowGraph;
    }
}