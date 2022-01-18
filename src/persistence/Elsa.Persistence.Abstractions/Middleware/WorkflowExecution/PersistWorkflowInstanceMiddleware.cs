using Elsa.Contracts;
using Elsa.Mediator.Contracts;
using Elsa.Models;
using Elsa.Persistence.Commands;
using Elsa.Persistence.Entities;
using Elsa.Persistence.Requests;
using Elsa.Pipelines.WorkflowExecution;
using Elsa.Pipelines.WorkflowExecution.Components;

namespace Elsa.Persistence.Middleware.WorkflowExecution;

public static class PersistWorkflowInstanceMiddlewareExtensions
{
    public static IWorkflowExecutionBuilder UsePersistence(this IWorkflowExecutionBuilder builder) => builder.UseMiddleware<PersistWorkflowInstanceMiddleware>();
}

/// <summary>
/// Takes care of persisting a workflow instance after workflow execution.
/// </summary>
public class PersistWorkflowInstanceMiddleware : WorkflowExecutionMiddleware
{
    private readonly IRequestSender _requestSender;
    private readonly ICommandSender _commandSender;
    private readonly IWorkflowStateSerializer _workflowStateSerializer;
    private readonly IPayloadSerializer _payloadSerializer;
    private readonly IIdentityGenerator _identityGenerator;
    private readonly ISystemClock _clock;

    public PersistWorkflowInstanceMiddleware(
        WorkflowMiddlewareDelegate next,
        IRequestSender requestSender,
        ICommandSender commandSender,
        IWorkflowStateSerializer workflowStateSerializer,
        IPayloadSerializer payloadSerializer,
        IIdentityGenerator identityGenerator,
        ISystemClock clock) : base(next)
    {
        _requestSender = requestSender;
        _commandSender = commandSender;
        _workflowStateSerializer = workflowStateSerializer;
        _payloadSerializer = payloadSerializer;
        _identityGenerator = identityGenerator;
        _clock = clock;
    }

    public override async ValueTask InvokeAsync(WorkflowExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;
        var workflow = context.Workflow;
        var (definitionId, version, definitionVersionId) = workflow.Identity;
        var existingWorkflowInstance = await _requestSender.RequestAsync(new FindWorkflowInstance(context.Id), cancellationToken);

        // Setup a new workflow instance if no existing instance was found.
        var workflowInstance = existingWorkflowInstance ?? new WorkflowInstance
        {
            Id = context.Id,
            DefinitionId = definitionId,
            Version = version,
            DefinitionVersionId = definitionVersionId,
            CreatedAt = _clock.UtcNow,
            WorkflowStatus = WorkflowStatus.Running,
            CorrelationId = _identityGenerator.GenerateId(),
            WorkflowState = _workflowStateSerializer.ReadState(context)
        };

        // Get a copy of current bookmarks.
        var existingBookmarks = await _requestSender.RequestAsync(new FindWorkflowBookmarks(workflowInstance.Id), cancellationToken);
        var bookmarksSnapshot = existingBookmarks.Select(x => x.ToBookmark()).ToList();

        // Exclude the current bookmark that initiated the creation of the workflow context, if any.
        var invokedBookmark = context.Bookmark;
        var removedBookmarkIds = new List<string>();

        if (invokedBookmark != null)
        {
            bookmarksSnapshot.RemoveAll(x => x.Id == invokedBookmark.Id);
            removedBookmarkIds.Add(invokedBookmark.Id);
        }

        // Apply bookmarks to workflow context.
        context.RegisterBookmarks(bookmarksSnapshot);

        // Persist workflow instance.
        await _commandSender.ExecuteAsync(new SaveWorkflowInstance(workflowInstance), cancellationToken);

        // Invoke next middleware.
        await Next(context);

        // Update workflow instance.
        var workflowState = _workflowStateSerializer.ReadState(context);
        workflowInstance.WorkflowState = workflowState;

        // Persist updated workflow instance.
        await _commandSender.ExecuteAsync(new SaveWorkflowInstance(workflowInstance), cancellationToken);

        // Remove bookmarks that were in the snapshot but no longer present in context.
        removedBookmarkIds.AddRange(bookmarksSnapshot.Except(context.Bookmarks).Select(x => x.Id));
        await _commandSender.ExecuteAsync(new DeleteWorkflowBookmarks(removedBookmarkIds), cancellationToken);

        // Persist bookmarks, if any.
        var workflowBookmarks = context.Bookmarks.Select(x => new WorkflowBookmark
        {
            Id = x.Id,
            WorkflowDefinitionId = definitionId,
            WorkflowInstanceId = workflowInstance.Id,
            CorrelationId = workflowInstance.CorrelationId,
            Hash = x.Hash,
            Payload = x.Payload,
            Name = x.Name,
            ActivityId = x.ActivityId,
            ActivityInstanceId = x.ActivityInstanceId,
            CallbackMethodName = x.CallbackMethodName
        }).ToList();

        await _commandSender.ExecuteAsync(new SaveWorkflowBookmarks(workflowBookmarks), cancellationToken);
    }
}