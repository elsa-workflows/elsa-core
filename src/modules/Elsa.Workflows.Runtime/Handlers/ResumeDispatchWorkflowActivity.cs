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
/// Resumes any blocking <see cref="DispatchWorkflow"/> activities when its child workflow completes.
/// </summary>
[PublicAPI]
internal class ResumeDispatchWorkflowActivity : INotificationHandler<WorkflowExecuted>
{
    private readonly IWorkflowInbox _workflowInbox;

    public ResumeDispatchWorkflowActivity(IWorkflowInbox workflowInbox)
    {
        _workflowInbox = workflowInbox;
    }

    public async Task HandleAsync(WorkflowExecuted notification, CancellationToken cancellationToken)
    {
        var workflowState = notification.WorkflowState;

        if (workflowState.Status != WorkflowStatus.Finished)
            return;

        var bookmark = new DispatchWorkflowBookmark(notification.WorkflowState.Id);
        var activityTypeName = ActivityTypeNameHelper.GenerateTypeName<DispatchWorkflow>();
        var input = workflowState.Output;
        var message = new NewWorkflowInboxMessage
        {
            ActivityTypeName = activityTypeName,
            Input = input,
            BookmarkPayload = bookmark,
        };

        await _workflowInbox.SubmitAsync(message, cancellationToken);
    }
}