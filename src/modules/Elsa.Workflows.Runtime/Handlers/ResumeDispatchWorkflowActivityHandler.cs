using Elsa.Mediator.Services;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Notifications;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Bookmarks;
using Elsa.Workflows.Runtime.Services;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Handlers;

/// <summary>
/// Resumes any blocking <see cref="DispatchWorkflow"/> activities when its child workflow completes.
/// </summary>
[PublicAPI]
internal class ResumeDispatchWorkflowActivityHandler : INotificationHandler<WorkflowExecuted>
{
    private readonly IWorkflowRuntime _workflowRuntime;

    public ResumeDispatchWorkflowActivityHandler(IWorkflowRuntime workflowRuntime)
    {
        _workflowRuntime = workflowRuntime;
    }
    
    public async Task HandleAsync(WorkflowExecuted notification, CancellationToken cancellationToken)
    {
        if (notification.WorkflowState.Status != WorkflowStatus.Finished)
            return;

        var bookmark = new DispatchWorkflowBookmark(notification.WorkflowState.Id);
        var activityTypeName = ActivityTypeNameHelper.GenerateTypeName<DispatchWorkflow>();
        await _workflowRuntime.TriggerWorkflowsAsync(activityTypeName, bookmark, new TriggerWorkflowsRuntimeOptions(), cancellationToken);
    }
}