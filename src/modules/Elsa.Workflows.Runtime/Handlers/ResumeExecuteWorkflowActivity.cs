using Elsa.Mediator.Contracts;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Notifications;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Stimuli;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Handlers;

/// <summary>
/// Resumes any blocking <see cref="ExecuteWorkflow"/> activities when its child workflow completes.
/// </summary>
[PublicAPI]
internal class ResumeExecuteWorkflowActivity(IBookmarkQueue bookmarkQueue, IStimulusHasher stimulusHasher) : INotificationHandler<WorkflowExecuted>
{
    private static readonly string ActivityTypeName = ActivityTypeNameHelper.GenerateTypeName<ExecuteWorkflow>();

    public async Task HandleAsync(WorkflowExecuted notification, CancellationToken cancellationToken)
    {
        var workflowState = notification.WorkflowState;

        if (workflowState.Status != WorkflowStatus.Finished)
            return;

        var stimulus = new ExecuteWorkflowStimulus(notification.WorkflowState.Id);
        var input = workflowState.Output;

        var bookmarkQueueItem = new NewBookmarkQueueItem
        {
            ActivityTypeName = ActivityTypeName,
            StimulusHash = stimulusHasher.Hash(ActivityTypeName, stimulus),
            Options = new ResumeBookmarkOptions
            {
                Input = input
            }
        };

        await bookmarkQueue.EnqueueAsync(bookmarkQueueItem, cancellationToken);
    }
}