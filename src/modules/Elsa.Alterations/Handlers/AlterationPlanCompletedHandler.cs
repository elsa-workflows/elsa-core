using Elsa.Alterations.Bookmarks;
using Elsa.Alterations.Core.Notifications;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Requests;
using JetBrains.Annotations;

namespace Elsa.Alterations.Handlers;

/// <summary>
/// Handles <see cref="AlterationPlanCompleted"/> notifications and triggers any workflows that are waiting for the plan to complete.
/// </summary>
[UsedImplicitly]
public class AlterationPlanCompletedHandler(IWorkflowDispatcher workflowDispatcher) : INotificationHandler<AlterationPlanCompleted>
{
    /// <inheritdoc />
    public async Task HandleAsync(AlterationPlanCompleted notification, CancellationToken cancellationToken)
    {
        // Trigger any workflow instances that are waiting for the plan to complete.
        var planId = notification.Plan.Id;
        var bookmarkPayload = new AlterationPlanCompletedPayload(planId);
        var triggerRequest = new DispatchTriggerWorkflowsRequest(ActivityTypeNameHelper.GenerateTypeName<Activities.AlterationPlanCompleted>(), bookmarkPayload);
        await workflowDispatcher.DispatchAsync(triggerRequest, cancellationToken);
    }
}