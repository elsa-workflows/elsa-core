using Elsa.Helpers;
using Elsa.Mediator.Services;
using Elsa.Models;
using Elsa.Persistence.Entities;
using Elsa.Persistence.Services;
using Elsa.Pipelines.WorkflowExecution;
using Elsa.Pipelines.WorkflowExecution.Components;
using Elsa.Services;
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
    private readonly IRequestSender _requestSender;
    private readonly IEventPublisher _eventPublisher;
    private readonly IWorkflowStateSerializer _workflowStateSerializer;
    private readonly IBookmarkManager _bookmarkManager;
    private readonly IIdentityGenerator _identityGenerator;
    private readonly ISystemClock _clock;

    public PersistWorkflowInstanceMiddleware(
        WorkflowMiddlewareDelegate next,
        IWorkflowInstanceStore workflowInstanceStore,
        IWorkflowBookmarkStore bookmarkStore,
        IRequestSender requestSender,
        IEventPublisher eventPublisher,
        IBookmarkManager bookmarkManager,
        IWorkflowStateSerializer workflowStateSerializer,
        IIdentityGenerator identityGenerator,
        ISystemClock clock) : base(next)
    {
        _workflowInstanceStore = workflowInstanceStore;
        _bookmarkStore = bookmarkStore;
        _requestSender = requestSender;
        _eventPublisher = eventPublisher;
        _bookmarkManager = bookmarkManager;
        _workflowStateSerializer = workflowStateSerializer;
        _identityGenerator = identityGenerator;
        _clock = clock;
    }

    public override async ValueTask InvokeAsync(WorkflowExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;
        var workflow = context.Workflow;
        var (definitionId, version, definitionVersionId) = workflow.Identity;
        var existingWorkflowInstance = await _workflowInstanceStore.FindByIdAsync(context.Id, cancellationToken);
        var workflowInstanceName = default(string?);
        var now = _clock.UtcNow;

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
            WorkflowState = _workflowStateSerializer.ReadState(context),
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

        // Invoke next middleware.
        await Next(context);

        // Update workflow instance.
        var workflowState = _workflowStateSerializer.ReadState(context);
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

        // Persist updated workflow instance.
        await _workflowInstanceStore.SaveAsync(workflowInstance, cancellationToken);

        // Get a diff between bookmarks that were in the snapshot but no longer present in context.
        var diff = Diff.For(bookmarksSnapshot, context.Bookmarks.ToList());

        // Delete removed bookmarks.
        var removedBookmarks = diff.Removed.Select(x => WorkflowBookmark.FromBookmark(x, workflowInstance)).ToList();
        await _bookmarkManager.DeleteBookmarksAsync(removedBookmarks, cancellationToken);

        // Persist created bookmarks.
        var createdBookmarks = diff.Added.Select(x => WorkflowBookmark.FromBookmark(x, workflowInstance)).ToList();
        await _bookmarkManager.SaveBookmarksAsync(createdBookmarks, cancellationToken);

        // Publish an event so that observers can update their workers.
        await _eventPublisher.PublishAsync(new WorkflowBookmarksIndexed(new IndexedWorkflowBookmarks(workflowState, createdBookmarks, removedBookmarks)), cancellationToken);
    }
}