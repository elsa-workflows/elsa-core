using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Entities;
using Elsa.Alterations.Core.Enums;
using Elsa.Alterations.Core.Models;
using Elsa.Alterations.Core.Notifications;
using Elsa.Common.Contracts;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.Alterations.Core.Services;

/// <summary>
/// Stores the new plan and schedules it for immediate execution.
/// </summary>
public class DefaultAlterationPlanScheduler : IAlterationPlanScheduler
{
    private readonly IAlterationPlanStore _alterationPlanStore;
    private readonly IAlterationJobStore _alterationJobStore;
    private readonly INotificationSender _notificationSender;
    private readonly IIdentityGenerator _identityGenerator;
    private readonly ISystemClock _systemClock;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultAlterationPlanScheduler"/> class.
    /// </summary>
    public DefaultAlterationPlanScheduler(
        IAlterationPlanStore alterationPlanStore,
        IAlterationJobStore alterationJobStore,
        INotificationSender notificationSender, 
        IIdentityGenerator identityGenerator, 
        ISystemClock systemClock)
    {
        _alterationPlanStore = alterationPlanStore;
        _alterationJobStore = alterationJobStore;
        _notificationSender = notificationSender;
        _identityGenerator = identityGenerator;
        _systemClock = systemClock;
    }

    /// <inheritdoc />
    public async Task<string> SubmitAsync(NewAlterationPlan newPlan, CancellationToken cancellationToken = default)
    {
        var now = _systemClock.UtcNow;
        var plan = new AlterationPlan
        {
            Id = _identityGenerator.GenerateId(),
            Alterations = newPlan.Alterations,
            WorkflowInstanceIds = newPlan.WorkflowInstanceIds,
            Status = AlterationPlanStatus.Pending,
            CreatedAt = now
        };

        await _alterationPlanStore.SaveAsync(plan, cancellationToken);

        var jobs = plan.WorkflowInstanceIds.Select(workflowInstanceId => new AlterationJob
            {
                Id = _identityGenerator.GenerateId(),
                PlanId = plan.Id,
                Status = AlterationJobStatus.Pending,
                WorkflowInstanceId = workflowInstanceId,
                CreatedAt = now
            })
            .ToList();

        await _alterationJobStore.SaveManyAsync(jobs, cancellationToken);
        await _notificationSender.SendAsync(new AlterationPlanSubmitted(plan), cancellationToken);
        return plan.Id;
    }
}