using Elsa.Mediator.Contracts;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Notifications;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Stimuli;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Handlers;

/// <summary>
/// Resumes any blocking <see cref="DispatchWorkflow"/> activities when its child workflow completes.
/// </summary>
[PublicAPI]
internal class ResumeDispatchWorkflowActivity(IBookmarkQueue bookmarkQueue, IStimulusHasher stimulusHasher, IStimulusSender stimulusSender) :
    INotificationHandler<WorkflowExecuted>,
    INotificationHandler<DispatchWorkflowActivationDenied>
{
    private static readonly string ActivityTypeName = ActivityTypeNameHelper.GenerateTypeName<DispatchWorkflow>();

    public async Task HandleAsync(WorkflowExecuted notification, CancellationToken cancellationToken)
    {
        var workflowState = notification.WorkflowState;

        if (workflowState.Status != WorkflowStatus.Finished)
            return;

        var props = workflowState.Properties;
        var waitForCompletion = props.TryGetValue("WaitForCompletion", out var waitForCompletionValue) && waitForCompletionValue is true;

        if (!waitForCompletion)
            return;

        var parentInstanceId = props.TryGetValue("ParentInstanceId", out var parentInstanceIdValue) ? parentInstanceIdValue as string : null;
        if (string.IsNullOrWhiteSpace(parentInstanceId))
            return;

        await EnqueueResumeAsync(parentInstanceId, workflowState.Id, workflowState.Output, cancellationToken);
    }

    public async Task HandleAsync(DispatchWorkflowActivationDenied notification, CancellationToken cancellationToken)
    {
        if (notification.ActivityTypeName != ActivityTypeName)
            return;

        var input = new Dictionary<string, object>
        {
            ["CannotStart"] = true,
            ["WorkflowInstanceId"] = notification.WorkflowInstanceId
        };
        await stimulusSender.SendAsync(notification.StimulusHash, new()
        {
            WorkflowInstanceId = notification.ParentWorkflowInstanceId,
            Input = input
        }, cancellationToken);
    }

    private async Task EnqueueResumeAsync(string parentInstanceId, string workflowInstanceId, IDictionary<string, object> input, CancellationToken cancellationToken)
    {
        var activityTypeName = ActivityTypeName;
        var stimulus = new DispatchWorkflowStimulus(workflowInstanceId);
        var stimulusHash = stimulusHasher.Hash(activityTypeName, stimulus);
        await EnqueueResumeAsync(parentInstanceId, activityTypeName, stimulusHash, input, cancellationToken);
    }

    private Task EnqueueResumeAsync(string parentInstanceId, string activityTypeName, string stimulusHash, IDictionary<string, object> input, CancellationToken cancellationToken)
    {
        var bookmarkQueueItem = new NewBookmarkQueueItem
        {
            WorkflowInstanceId = parentInstanceId,
            ActivityTypeName = activityTypeName,
            StimulusHash = stimulusHash,
            Options = new()
            {
                Input = input
            }
        };

        return bookmarkQueue.EnqueueAsync(bookmarkQueueItem, cancellationToken);
    }
}
