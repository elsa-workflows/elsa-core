using Elsa.Mediator.Contracts;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Notifications;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Bookmarks;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Handlers;

/// <summary>
/// Resumes any blocking <see cref="ExecuteWorkflow"/> activities when its child workflow completes.
/// </summary>
[PublicAPI]
internal class ResumeExecuteWorkflowActivity(IWorkflowInbox bookmarkQueue) : INotificationHandler<WorkflowExecuted>
{
    private static readonly string ActivityTypeName = ActivityTypeNameHelper.GenerateTypeName<ExecuteWorkflow>();

    public async Task HandleAsync(WorkflowExecuted notification, CancellationToken cancellationToken)
    {
        var workflowState = notification.WorkflowState;

        if (workflowState.Status != WorkflowStatus.Finished)
            return;

        var workflowInstanceId = notification.WorkflowState.Id;
        var payload = new ExecuteWorkflowPayload(workflowInstanceId);
        var input = workflowState.Output;

        var message = new NewWorkflowInboxMessage
        {
            ActivityTypeName = ActivityTypeName,
            BookmarkPayload = payload,
            Input = input,
        };

        await bookmarkQueue.SubmitAsync(message, cancellationToken);
    }
}