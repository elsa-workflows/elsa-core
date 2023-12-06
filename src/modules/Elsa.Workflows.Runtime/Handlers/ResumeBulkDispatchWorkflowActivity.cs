using Elsa.Mediator.Contracts;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Notifications;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Bookmarks;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Handlers;

/// <summary>
/// Resumes any blocking <see cref="BulkDispatchWorkflows"/> activities when its child workflows complete.
/// </summary>
[PublicAPI]
internal class ResumeBulkDispatchWorkflowActivity(IWorkflowInbox workflowInbox) : INotificationHandler<WorkflowExecuted>
{
    public async Task HandleAsync(WorkflowExecuted notification, CancellationToken cancellationToken)
    {
        var workflowState = notification.WorkflowState;

        if (workflowState.Status != WorkflowStatus.Finished)
            return;

        var parentInstanceId = workflowState.Properties.TryGetValue("ParentInstanceId", out var parentInstanceIdValue) ? parentInstanceIdValue.ToString() : default;

        if (string.IsNullOrWhiteSpace(parentInstanceId))
            return;

        var bookmark = new BulkDispatchWorkflowsBookmark(parentInstanceId);
        var activityTypeName = ActivityTypeNameHelper.GenerateTypeName<BulkDispatchWorkflows>();
        var input = new Dictionary<string, object>
        {
            ["WorkflowOutput"] = workflowState.Output,
            ["WorkflowInstanceId"] = workflowState.Id
        };
        var message = new NewWorkflowInboxMessage
        {
            ActivityTypeName = activityTypeName,
            Input = input,
            BookmarkPayload = bookmark,
        };

        await workflowInbox.SubmitAsync(message, cancellationToken);
    }
}