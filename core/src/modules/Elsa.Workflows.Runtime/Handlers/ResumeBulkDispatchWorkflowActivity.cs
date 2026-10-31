using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Workflows;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Notifications;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Stimuli;
using Elsa.Workflows.State;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Handlers;

/// <summary>
/// Resumes any blocking <see cref="BulkDispatchWorkflows"/> activities when its child workflows complete, including when a
/// child is cancelled through <see cref="IWorkflowCanceler"/>, which does not publish <see cref="WorkflowExecuted"/>.
/// </summary>
[PublicAPI]
internal class ResumeBulkDispatchWorkflowActivity(IBookmarkQueue bookmarkQueue, IStimulusHasher stimulusHasher, IStimulusSender stimulusSender) :
    INotificationHandler<WorkflowExecuted>,
    INotificationHandler<WorkflowCancelled>,
    INotificationHandler<DispatchWorkflowActivationDenied>
{
    private static readonly string ActivityTypeName = ActivityTypeNameHelper.GenerateTypeName<BulkDispatchWorkflows>();

    public Task HandleAsync(WorkflowExecuted notification, CancellationToken cancellationToken) =>
        ResumeParentAsync(notification.WorkflowState, null, cancellationToken);

    public Task HandleAsync(WorkflowCancelled notification, CancellationToken cancellationToken) =>
        // Cancelling a workflow that had already finished or faulted leaves its sub-status as it was, and its completion
        // already resumed the parent through WorkflowExecuted. Every BulkDispatchWorkflows of a parent waits on the same
        // stimulus, so the resume targets the activity instance the child names: cancelling one bulk activity cancels its
        // children, and those must not count toward a sibling bulk activity, nor may a child that no bulk activity dispatched.
        notification.WorkflowState is { SubStatus: WorkflowSubStatus.Cancelled } workflowState
        && workflowState.Properties.TryGetValue<string>(WorkflowDispatchProperties.WaitingActivityInstanceId, out var waitingActivityInstanceId)
        && !string.IsNullOrWhiteSpace(waitingActivityInstanceId)
            ? ResumeParentAsync(workflowState, waitingActivityInstanceId, cancellationToken)
            : Task.CompletedTask;

    private async Task ResumeParentAsync(WorkflowState workflowState, string? waitingActivityInstanceId, CancellationToken cancellationToken)
    {
        if (workflowState.Status != WorkflowStatus.Finished)
            return;

        var waitForCompletion = workflowState.Properties.TryGetValue(WorkflowDispatchProperties.WaitForCompletion, out var waitForCompletionValue) && waitForCompletionValue is true;
        
        if (!waitForCompletion)
            return;
        
        var parentInstanceId = workflowState.Properties.TryGetValue("ParentInstanceId", out var parentInstanceIdValue) ? parentInstanceIdValue as string : null;
        if (string.IsNullOrWhiteSpace(parentInstanceId))
            return;
        
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
            ActivityInstanceId = waitingActivityInstanceId,
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
