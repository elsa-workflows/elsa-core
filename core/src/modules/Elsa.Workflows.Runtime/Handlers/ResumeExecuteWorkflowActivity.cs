using Elsa.Mediator.Contracts;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Notifications;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Stimuli;
using Elsa.Workflows.State;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Handlers;

/// <summary>
/// Resumes any blocking <see cref="ExecuteWorkflow"/> activities when its child workflow completes, including when the child
/// is cancelled through <see cref="IWorkflowCanceler"/>, which does not publish <see cref="WorkflowExecuted"/>.
/// </summary>
[UsedImplicitly]
internal class ResumeExecuteWorkflowActivity(IBookmarkQueue bookmarkQueue, IStimulusHasher stimulusHasher) :
    INotificationHandler<WorkflowExecuted>,
    INotificationHandler<WorkflowCancelled>
{
    private static readonly string ActivityTypeName = ActivityTypeNameHelper.GenerateTypeName<ExecuteWorkflow>();

    public Task HandleAsync(WorkflowExecuted notification, CancellationToken cancellationToken) =>
        ResumeParentAsync(notification.WorkflowState, cancellationToken);

    public Task HandleAsync(WorkflowCancelled notification, CancellationToken cancellationToken) =>
        // Cancelling a workflow that had already finished or faulted leaves its sub-status as it was, and its completion
        // already resumed the parent through WorkflowExecuted.
        notification.WorkflowState is { SubStatus: WorkflowSubStatus.Cancelled } workflowState
            ? ResumeParentAsync(workflowState, cancellationToken)
            : Task.CompletedTask;

    private async Task ResumeParentAsync(WorkflowState workflowState, CancellationToken cancellationToken)
    {
        if (workflowState.Status != WorkflowStatus.Finished)
            return;

        var props = workflowState.Properties;
        var waitForCompletion = props.TryGetValue(WorkflowDispatchProperties.WaitForCompletion, out var waitForCompletionValue) && waitForCompletionValue is true;

        if (!waitForCompletion)
            return;

        var parentInstanceId = props.TryGetValue("ParentInstanceId", out var parentInstanceIdValue) ? parentInstanceIdValue as string : null;
        if (string.IsNullOrWhiteSpace(parentInstanceId))
            return;

        var stimulus = new ExecuteWorkflowStimulus(workflowState.Id);
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