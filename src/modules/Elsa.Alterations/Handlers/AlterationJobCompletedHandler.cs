using Elsa.Alterations.Activities;
using Elsa.Alterations.Bookmarks;
using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Enums;
using Elsa.Alterations.Core.Filters;
using Elsa.Alterations.Core.Notifications;
using Elsa.Common.Contracts;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Requests;
using JetBrains.Annotations;

namespace Elsa.Alterations.Handlers;

/// <summary>
/// Handles <see cref="AlterationJobCompleted"/> notifications and updates the plan status if all jobs are completed.
/// </summary>
[UsedImplicitly]
public class AlterationJobCompletedHandler : INotificationHandler<AlterationJobCompleted>
{
    private readonly IWorkflowDispatcher _workflowDispatcher;
    private readonly IAlterationPlanStore _alterationPlanStore;
    private readonly IAlterationJobStore _alterationJobStore;
    private readonly ISystemClock _systemClock;

    /// <summary>
    /// Initializes a new instance of the <see cref="AlterationJobCompletedHandler"/> class.
    /// </summary>
    public AlterationJobCompletedHandler(IWorkflowDispatcher workflowDispatcher, IAlterationPlanStore alterationPlanStore, IAlterationJobStore alterationJobStore, ISystemClock systemClock)
    {
        _workflowDispatcher = workflowDispatcher;
        _alterationPlanStore = alterationPlanStore;
        _alterationJobStore = alterationJobStore;
        _systemClock = systemClock;
    }

    /// <inheritdoc />
    public async Task HandleAsync(AlterationJobCompleted notification, CancellationToken cancellationToken)
    {
        var job = notification.Job;
        var planId = job.PlanId;
        var planFilter = new AlterationPlanFilter { Id = planId };
        var plan = (await _alterationPlanStore.FindAsync(planFilter, cancellationToken))!;

        // Check if all jobs are completed.
        var jobFilter = new AlterationJobFilter
        {
            PlanId = planId,
            Statuses = new[] { AlterationJobStatus.Pending, AlterationJobStatus.Running }
        };
        
        var allJobsCompleted = await _alterationJobStore.CountAsync(jobFilter, cancellationToken) == 0;
        
        if(!allJobsCompleted)
            return;
        
        // Update plan status.
        plan.Status = AlterationPlanStatus.Completed;
        plan.CompletedAt = _systemClock.UtcNow;
        
        await _alterationPlanStore.SaveAsync(plan, cancellationToken);
        
        // Trigger any workflow instances that are waiting for the plan to complete.
        var bookmarkPayload = new AlterationPlanCompletedPayload(planId);
        var triggerRequest = new DispatchTriggerWorkflowsRequest(nameof(AlterationPlanCompleted), bookmarkPayload);
        await _workflowDispatcher.DispatchAsync(triggerRequest, cancellationToken);
    }
}