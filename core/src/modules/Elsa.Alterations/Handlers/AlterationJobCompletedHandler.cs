using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Notifications;
using Elsa.Mediator.Contracts;
using JetBrains.Annotations;

namespace Elsa.Alterations.Handlers;

/// <summary>
/// Handles <see cref="AlterationJobCompleted"/> notifications and updates the plan status if all jobs are completed.
/// </summary>
[UsedImplicitly]
public class AlterationJobCompletedHandler(IAlterationPlanManager manager) : INotificationHandler<AlterationJobCompleted>
{
    /// <inheritdoc />
    public async Task HandleAsync(AlterationJobCompleted notification, CancellationToken cancellationToken)
    {
        var job = notification.Job;
        var planId = job.PlanId;
        var plan = (await manager.GetPlanAsync(planId, cancellationToken))!;
        var allJobsCompleted = await manager.GetIsAllJobsCompletedAsync(planId, cancellationToken);
        
        if(!allJobsCompleted)
            return;
        
        await manager.CompletePlanAsync(plan, cancellationToken);
    }
}