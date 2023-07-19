using Elsa.Mediator.Contracts;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Notifications;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Bookmarks;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Requests;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Handlers;

/// <summary>
/// Resumes any blocking <see cref="DispatchWorkflow"/> activities when its child workflow completes.
/// </summary>
[PublicAPI]
internal class ResumeDispatchWorkflowActivityHandler : INotificationHandler<WorkflowExecuted>
{
    private readonly IWorkflowDispatcher _workflowRuntime;

    public ResumeDispatchWorkflowActivityHandler(IWorkflowDispatcher workflowRuntime)
    {
        _workflowRuntime = workflowRuntime;
    }

    public async Task HandleAsync(WorkflowExecuted notification, CancellationToken cancellationToken)
    {
        var workflowState = notification.WorkflowState;

        if (workflowState.Status != WorkflowStatus.Finished)
            return;

        var bookmark = new DispatchWorkflowBookmark(notification.WorkflowState.Id);
        var activityTypeName = ActivityTypeNameHelper.GenerateTypeName<DispatchWorkflow>();
        var input = workflowState.Output;
        var request = new DispatchResumeWorkflowsRequest(activityTypeName, bookmark, Input: input);
        await _workflowRuntime.DispatchAsync(request, cancellationToken);
    }
}