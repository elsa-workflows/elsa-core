using Elsa.Mediator.Contracts;
using Elsa.Workflows.Notifications;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Stimuli;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Handlers;

/// <summary>
/// Resumes any blocking <see cref="DispatchWorkflow"/> activities when its child workflow completes.
/// </summary>
[PublicAPI]
internal class ResumeDispatchWorkflowActivity(IBookmarkResumer bookmarkResumer) : INotificationHandler<WorkflowExecuted>
{
    public async Task HandleAsync(WorkflowExecuted notification, CancellationToken cancellationToken)
    {
        var workflowState = notification.WorkflowState;

        if (workflowState.Status != WorkflowStatus.Finished)
            return;

        var stimulus = new DispatchWorkflowStimulus(notification.WorkflowState.Id);
        var input = workflowState.Output;

        var resumeOptions = new ResumeBookmarkOptions
        {
            Input = input
        };
        await bookmarkResumer.ResumeAsync<DispatchWorkflow>(stimulus, resumeOptions, cancellationToken);
    }
}