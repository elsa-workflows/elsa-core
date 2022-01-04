using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Contracts;
using Elsa.Mediator.Contracts;
using Elsa.Models;
using Elsa.Persistence.Commands;
using Elsa.Persistence.Entities;
using Elsa.Persistence.Requests;
using Elsa.Pipelines.WorkflowExecution;

namespace Elsa.Persistence.Middleware.WorkflowExecution;

public static class PersistWorkflowInstanceMiddlewareExtensions
{
    public static IWorkflowExecutionBuilder PersistWorkflows(this IWorkflowExecutionBuilder builder) => builder.UseMiddleware<PersistWorkflowInstanceMiddleware>();
}

/// <summary>
/// Takes care of persisting a workflow instance after workflow execution.
/// </summary>
public class PersistWorkflowInstanceMiddleware : IWorkflowExecutionMiddleware
{
    private readonly WorkflowMiddlewareDelegate _next;
    private readonly IRequestSender _requestSender;
    private readonly ICommandSender _commandSender;
    private readonly IWorkflowStateSerializer _workflowStateSerializer;
    private readonly IIdentityGenerator _identityGenerator;
    private readonly ISystemClock _clock;

    public PersistWorkflowInstanceMiddleware(
        WorkflowMiddlewareDelegate next,
        IRequestSender requestSender,
        ICommandSender commandSender,
        IWorkflowStateSerializer workflowStateSerializer,
        IIdentityGenerator identityGenerator,
        ISystemClock clock)
    {
        _next = next;
        _requestSender = requestSender;
        _commandSender = commandSender;
        _workflowStateSerializer = workflowStateSerializer;
        _identityGenerator = identityGenerator;
        _clock = clock;
    }

    public async ValueTask InvokeAsync(WorkflowExecutionContext context)
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
        var bookmarksSnapshot = existingBookmarks.Select(x => new Bookmark(x.Id, x.Name, x.Hash, x.ActivityId, x.ActivityInstanceId, x.Data, x.CallbackMethodName)).ToList();

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
        await _next(context);

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
            Data = x.Data,
            Name = x.Name,
            ActivityId = x.ActivityId,
            ActivityInstanceId = x.ActivityInstanceId,
            CallbackMethodName = x.CallbackMethodName
        }).ToList();

        await _commandSender.ExecuteAsync(new SaveWorkflowBookmarks(workflowBookmarks), cancellationToken);
    }
}