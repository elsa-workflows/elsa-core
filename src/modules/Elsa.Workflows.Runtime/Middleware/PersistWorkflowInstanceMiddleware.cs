using Elsa.Common.Services;
using Elsa.Mediator.Services;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Pipelines.WorkflowExecution;
using Elsa.Workflows.Core.Pipelines.WorkflowExecution.Components;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Persistence.Entities;
using Elsa.Workflows.Persistence.Services;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.Workflows.Runtime.Middleware;

/// <summary>
/// Takes care of persisting a workflow instance after workflow execution.
/// </summary>
public class PersistWorkflowInstanceMiddleware : WorkflowExecutionMiddleware
{
    public static readonly object WorkflowInstanceKey = new();
    public static readonly object WorkflowInstanceNameKey = new();

    private readonly IWorkflowInstanceStore _workflowInstanceStore;
    private readonly IWorkflowBookmarkStore _bookmarkStore;
    private readonly IEventPublisher _eventPublisher;
    private readonly IWorkflowStateSerializer _workflowStateSerializer;
    private readonly IStorageDriverManager _storageDriverManager;
    private readonly IBookmarkManager _bookmarkManager;
    private readonly IIdentityGenerator _identityGenerator;
    private readonly ISystemClock _systemClock;

    public PersistWorkflowInstanceMiddleware(
        WorkflowMiddlewareDelegate next,
        IWorkflowInstanceStore workflowInstanceStore,
        IWorkflowBookmarkStore bookmarkStore,
        IEventPublisher eventPublisher,
        IBookmarkManager bookmarkManager,
        IWorkflowStateSerializer workflowStateSerializer,
        IStorageDriverManager storageDriverManager,
        IIdentityGenerator identityGenerator,
        ISystemClock systemClock) : base(next)
    {
        _workflowInstanceStore = workflowInstanceStore;
        _bookmarkStore = bookmarkStore;
        _eventPublisher = eventPublisher;
        _bookmarkManager = bookmarkManager;
        _workflowStateSerializer = workflowStateSerializer;
        _storageDriverManager = storageDriverManager;
        _identityGenerator = identityGenerator;
        _systemClock = systemClock;
    }

    public override async ValueTask InvokeAsync(WorkflowExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;
        var workflow = context.Workflow;
        var (definitionId, version, definitionVersionId) = workflow.Identity;
        var existingWorkflowInstance = await _workflowInstanceStore.FindByIdAsync(context.Id, cancellationToken);
        var workflowInstanceName = default(string?);
        var now = _systemClock.UtcNow;

        // Get the workflow instance name, if any (could be provided by previously executed middleware). 
        if (context.TransientProperties.TryGetValue(WorkflowInstanceNameKey, out var name))
            workflowInstanceName = (string?)name;

        // Setup a new workflow instance if no existing instance was found.
        var workflowInstance = existingWorkflowInstance ?? new WorkflowInstance
        {
            Id = context.Id,
            DefinitionId = definitionId,
            Version = version,
            DefinitionVersionId = definitionVersionId,
            CreatedAt = now,
            Status = WorkflowStatus.Running,
            SubStatus = WorkflowSubStatus.Executing,
            CorrelationId = _identityGenerator.GenerateId(),
            WorkflowState = _workflowStateSerializer.SerializeState(context),
            Name = workflowInstanceName
        };

        // Store the workflow instance in the workflow execution context for other (middleware) components to use.
        context.TransientProperties[WorkflowInstanceKey] = workflowInstance;

        // Get a copy of current bookmarks.
        var existingBookmarks = await _bookmarkStore.FindManyByWorkflowInstanceIdAsync(workflowInstance.Id, cancellationToken);
        var bookmarksSnapshot = existingBookmarks.Select(x => x.ToBookmark()).ToList();
        var bookmarks = bookmarksSnapshot.ToList();

        // Exclude the current bookmark that initiated the creation of the workflow context, if any.
        var invokedBookmark = context.Bookmark;
        if (invokedBookmark != null) bookmarks.RemoveAll(x => x.Id == invokedBookmark.Id);

        // Apply bookmarks to workflow context.
        context.RegisterBookmarks(bookmarks);

        // Persist workflow instance.
        await _workflowInstanceStore.SaveAsync(workflowInstance, cancellationToken);
        
        // Load persistent variables.
        var dataDriveContext = new DataDriveContext(workflowInstance.WorkflowState, cancellationToken);
        
        foreach (var variableState in workflowInstance.WorkflowState.PersistentVariables)
        {
            var drive = _storageDriverManager.GetDriveById(variableState.StorageDriverId);
            if (drive == null) continue;
            var id = $"{context.Id}:{variableState.Name}";
            var value = await drive.ReadAsync(id, dataDriveContext);
            if (value == null) continue;
            var variable = new Variable(variableState.Name, value);
            context.MemoryRegister.Declare(variable);
        }

        // Invoke next middleware.
        await Next(context);

        // Update workflow instance.
        var workflowState = _workflowStateSerializer.SerializeState(context);
        workflowInstance.WorkflowState = workflowState;
        workflowInstance.Status = workflowState.Status;
        workflowInstance.SubStatus = workflowState.SubStatus;
        workflowInstance.CorrelationId = workflowState.CorrelationId;
        workflowInstance.LastExecutedAt = now;

        // Update timestamps.
        if (workflowInstance.Status == WorkflowStatus.Finished)
        {
            switch (workflowInstance.SubStatus)
            {
                case WorkflowSubStatus.Cancelled:
                    workflowInstance.CancelledAt = now;
                    break;
                case WorkflowSubStatus.Faulted:
                    workflowInstance.FaultedAt = now;
                    break;
                case WorkflowSubStatus.Finished:
                    workflowInstance.FinishedAt = now;
                    break;
            }
        }

        // Get the workflow instance name, if any (could be provided by previously executed middleware). 
        if (context.TransientProperties.TryGetValue(WorkflowInstanceNameKey, out name))
            workflowInstance.Name = (string?)name;

        // Persist variables.
        dataDriveContext = new DataDriveContext(workflowState, cancellationToken);
        
        foreach (var variableState in workflowState.PersistentVariables)
        {
            var drive = _storageDriverManager.GetDriveById(variableState.StorageDriverId);
            if (drive == null) continue;
            if (!context.MemoryRegister.TryGetBlock(variableState.Name, out var block)) continue;
            if (block.Value == null) continue;
            var id = $"{context.Id}:{variableState.Name}";
            await drive.WriteAsync(id, block.Value, dataDriveContext);
        }

        // Persist updated workflow instance.
        await _workflowInstanceStore.SaveAsync(workflowInstance, cancellationToken);

        // Get a diff between bookmarks that were in the snapshot but no longer present in context.
        var diff = Diff.For(bookmarksSnapshot, context.Bookmarks.ToList());

        // Delete removed bookmarks.
        var removedBookmarks = diff.Removed;
        //await _bookmarkManager.DeleteBookmarksAsync(removedBookmarks, cancellationToken);

        // Persist created bookmarks.
        var createdBookmarks = diff.Added;
        //await _bookmarkManager.SaveBookmarksAsync(createdBookmarks, cancellationToken);

        // Publish an event so that observers can update their workers.
        await _eventPublisher.PublishAsync(new WorkflowBookmarksIndexed(new IndexedWorkflowBookmarks(workflowState, createdBookmarks, removedBookmarks)), cancellationToken);
    }
}