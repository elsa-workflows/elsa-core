using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Entities;
using Elsa.Alterations.Core.Enums;
using Elsa.Alterations.Core.Filters;
using Elsa.Alterations.Core.Notifications;
using Elsa.Mediator.Contracts;
using JetBrains.Annotations;

namespace Elsa.Alterations.Handlers;

/// <summary>
/// Handles <see cref="AlterationPlanSubmitted"/> notifications and runs the plan in the background.
/// </summary>
[UsedImplicitly]
public class AlterationPlanSubmittedHandler : INotificationHandler<AlterationPlanSubmitted>
{
    private readonly IAlterationPlanStore _alterationPlanStore;
    private readonly IAlterationJobStore _alterationJobStore;
    private readonly IAlterationJobDispatcher _alterationJobDispatcher;
    private readonly IJobQueue _jobQueue;

    /// <summary>
    /// Initializes a new instance of the <see cref="AlterationPlanSubmittedHandler"/> class.
    /// </summary>
    public AlterationPlanSubmittedHandler(
        IAlterationPlanStore alterationPlanStore, 
        IAlterationJobStore alterationJobStore, 
        IAlterationJobDispatcher alterationJobDispatcher,
        IJobQueue jobQueue)
    {
        _alterationPlanStore = alterationPlanStore;
        _alterationJobStore = alterationJobStore;
        _alterationJobDispatcher = alterationJobDispatcher;
        _jobQueue = jobQueue;
    }

    /// <inheritdoc />
    public Task HandleAsync(AlterationPlanSubmitted notification, CancellationToken cancellationToken)
    {
        _jobQueue.Enqueue(ct => ExecutePlanAsync(notification.Plan, ct));
        return Task.CompletedTask;
    }

    private async Task ExecutePlanAsync(AlterationPlan plan, CancellationToken cancellationToken)
    {
        // If status is already running, do nothing.
        if (plan.Status == AlterationPlanStatus.Running)
            return;

        // Update status.
        plan.Status = AlterationPlanStatus.Running;
        await _alterationPlanStore.SaveAsync(plan, cancellationToken);

        // Create a job for each job in the plan.
        var filter = new AlterationJobFilter { PlanId = plan.Id };
        var alterationJobIds = await _alterationJobStore.FindManyIdsAsync(filter, cancellationToken);

        // Enqueue each job.
        foreach (var jobId in alterationJobIds) 
            await _alterationJobDispatcher.DispatchAsync(jobId, cancellationToken);
    }
}