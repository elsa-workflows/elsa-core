using Elsa.Mediator.Contracts;
using Elsa.Workflows;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Notifications;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Stimuli;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Handlers;

/// <summary>
/// Resumes any blocking <see cref="BulkDispatchWorkflows"/> activities when its child workflows complete.
/// </summary>
[PublicAPI]
internal class ResumeBulkDispatchWorkflowActivity(IBookmarkQueue bookmarkQueue, IStimulusHasher stimulusHasher, IStimulusSender stimulusSender) :
    INotificationHandler<WorkflowExecuted>,
    INotificationHandler<DispatchWorkflowActivationDenied>
{
    private static readonly string ActivityTypeName = ActivityTypeNameHelper.GenerateTypeName<BulkDispatchWorkflows>();

    public async Task HandleAsync(WorkflowExecuted notification, CancellationToken cancellationToken)
    {
        var workflowState = notification.WorkflowState;

        if (workflowState.Status != WorkflowStatus.Finished)
            return;

        var waitForCompletion = workflowState.Properties.TryGetValue("WaitForCompletion", out var waitForCompletionValue) && (bool)waitForCompletionValue;
        
        if (!waitForCompletion)
            return;
        
        if (!workflowState.Properties.TryGetValue("ParentInstanceId", out var parentInstanceIdValue))
            return;
        
        var parentInstanceId = (string)parentInstanceIdValue;
        var activityTypeName = ActivityTypeName;
        var stimulus = new BulkDispatchWorkflowsStimulus(parentInstanceId);
        var stimulusHash = stimulusHasher.Hash(activityTypeName, stimulus);
        var workflowInstanceId = workflowState.Id;
        var input = new Dictionary<string, object>
        {
            ["WorkflowOutput"] = workflowState.Output,
            ["WorkflowInstanceId"] = workflowInstanceId,
            ["WorkflowStatus"] = workflowState.Status,
            ["WorkflowSubStatus"] = workflowState.SubStatus,
        };

        var resumeBookmarkOptions = new ResumeBookmarkOptions
        {
            Input = input
        };
        var bookmarkQueueItem = new NewBookmarkQueueItem
        {
            WorkflowInstanceId = parentInstanceId,
            ActivityTypeName = activityTypeName,
            StimulusHash = stimulusHash,
            Options = resumeBookmarkOptions
        };
        await bookmarkQueue.EnqueueAsync(bookmarkQueueItem, cancellationToken);
    }

    public async Task HandleAsync(DispatchWorkflowActivationDenied notification, CancellationToken cancellationToken)
    {
        if (notification.ActivityTypeName != ActivityTypeName)
            return;

        var input = new Dictionary<string, object>
        {
            ["CannotStart"] = true,
            ["WorkflowOutput"] = new Dictionary<string, object>(),
            ["WorkflowInstanceId"] = notification.WorkflowInstanceId,
            ["WorkflowStatus"] = WorkflowStatus.Finished,
            ["WorkflowSubStatus"] = WorkflowSubStatus.Faulted
        };
        await stimulusSender.SendAsync(notification.StimulusHash, new()
        {
            WorkflowInstanceId = notification.ParentWorkflowInstanceId,
            Input = input
        }, cancellationToken);
    }
}
