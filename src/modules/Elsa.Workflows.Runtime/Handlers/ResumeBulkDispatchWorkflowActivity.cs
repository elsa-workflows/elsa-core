using Elsa.Mediator.Contracts;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Notifications;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Stimuli;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Handlers;

/// <summary>
/// Resumes any blocking <see cref="BulkDispatchWorkflows"/> activities when its child workflows complete.
/// </summary>
[PublicAPI]
internal class ResumeBulkDispatchWorkflowActivity(IBookmarkQueue bookmarkResumer, IStimulusHasher stimulusHasher) : INotificationHandler<WorkflowExecuted>
{
    public async Task HandleAsync(WorkflowExecuted notification, CancellationToken cancellationToken)
    {
        var workflowState = notification.WorkflowState;

        if (workflowState.Status != WorkflowStatus.Finished)
            return;

        var parentInstanceId = workflowState.Properties.TryGetValue("ParentInstanceId", out var parentInstanceIdValue) ? parentInstanceIdValue.ToString() : default;

        if (string.IsNullOrWhiteSpace(parentInstanceId))
            return;

        var activityTypeName = ActivityTypeNameHelper.GenerateTypeName<BulkDispatchWorkflows>();
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
        await bookmarkResumer.EnqueueAsync(bookmarkQueueItem, cancellationToken);
    }
}