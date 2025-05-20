using Elsa.Mediator.Contracts;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Notifications;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Stimuli;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime.Handlers;

/// <summary>
/// Resumes any blocking <see cref="DispatchWorkflow"/> activities when its child workflow completes.
/// </summary>
[PublicAPI]
internal class ResumeDispatchWorkflowActivity(IBookmarkQueue bookmarkQueue, IStimulusHasher stimulusHasher, ILogger<ResumeDispatchWorkflowActivity> logger) : INotificationHandler<WorkflowExecuted>
{
    private static readonly string ActivityTypeName = ActivityTypeNameHelper.GenerateTypeName<DispatchWorkflow>();

    public async Task HandleAsync(WorkflowExecuted notification, CancellationToken cancellationToken)
    {
        var workflowState = notification.WorkflowState;

        if (workflowState.Status != WorkflowStatus.Finished)
            return;

        var props = workflowState.Properties;
        var waitForCompletion = props.TryGetValue("WaitForCompletion", out var waitForCompletionValue) && (bool)waitForCompletionValue;

        if (!waitForCompletion)
            return;

        var parentInstanceId = (string)props["ParentInstanceId"];
        var stimulus = new DispatchWorkflowStimulus(notification.WorkflowState.Id);
        var input = workflowState.Output;

        var bookmarkQueueItem = new NewBookmarkQueueItem
        {
            WorkflowInstanceId = parentInstanceId,
            ActivityTypeName = ActivityTypeName,
            StimulusHash = stimulusHasher.Hash(ActivityTypeName, stimulus),
            Options = new()
            {
                Input = input
            }
        };

        await bookmarkQueue.EnqueueAsync(bookmarkQueueItem, cancellationToken);
    }
}