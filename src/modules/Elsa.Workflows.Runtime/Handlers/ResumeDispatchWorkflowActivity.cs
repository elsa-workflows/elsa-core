using Elsa.Mediator.Contracts;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Notifications;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Options;
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
        
        logger.LogDebug("Handling workflow executed notification for workflow {WorkflowInstanceId}", notification.WorkflowState.Id);

        if (workflowState.Status != WorkflowStatus.Finished)
        {
            logger.LogDebug("Workflow {WorkflowInstanceId} is not in a finished state. Skipping resumption of any blocking DispatchWorkflow activities", notification.WorkflowState.Id);
            return;
        }

        var stimulus = new DispatchWorkflowStimulus(notification.WorkflowState.Id);
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
        
        logger.LogDebug("Resuming any blocking DispatchWorkflow activities for workflow {WorkflowInstanceId} using stimulus hash {StimulusHash}", notification.WorkflowState.Id, bookmarkQueueItem.StimulusHash);
        await bookmarkQueue.EnqueueAsync(bookmarkQueueItem, cancellationToken);
    }
}