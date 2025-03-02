using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Entities;
using Elsa.Alterations.Core.Enums;
using Elsa.Alterations.Core.Filters;
using Elsa.Alterations.Core.Notifications;
using Elsa.Common;
using Elsa.Mediator.Contracts;

namespace Elsa.Alterations.Services;

/// <inheritdoc />
public class AlterationPlanManager(IAlterationPlanStore planStore, IAlterationJobStore jobStore, ISystemClock systemClock, INotificationSender notificationSender) : IAlterationPlanManager
{
    /// <inheritdoc />
    public async Task<AlterationPlan?> GetPlanAsync(string planId, CancellationToken cancellationToken = default)
    {
        var planFilter = new AlterationPlanFilter { Id = planId };
        return await planStore.FindAsync(planFilter, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> GetIsAllJobsCompletedAsync(string planId, CancellationToken cancellationToken = default)
    {
        // Check if all jobs are completed.
        var jobFilter = new AlterationJobFilter
        {
            PlanId = planId,
            Statuses = new[] { AlterationJobStatus.Pending, AlterationJobStatus.Running }
        };
        
        return await jobStore.CountAsync(jobFilter, cancellationToken) == 0;
    }

    /// <inheritdoc />
    public async Task CompletePlanAsync(AlterationPlan plan, CancellationToken cancellationToken = default)
    {
        plan.Status = AlterationPlanStatus.Completed;
        plan.CompletedAt = systemClock.UtcNow;
        
        await planStore.SaveAsync(plan, cancellationToken);
        await notificationSender.SendAsync(new AlterationPlanCompleted(plan), cancellationToken);
    }
}