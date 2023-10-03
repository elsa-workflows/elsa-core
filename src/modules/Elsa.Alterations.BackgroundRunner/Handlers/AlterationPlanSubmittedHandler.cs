using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Entities;
using Elsa.Alterations.Core.Enums;
using Elsa.Alterations.Core.Filters;
using Elsa.Alterations.Core.Notifications;
using Elsa.Mediator.Contracts;

namespace Elsa.Alterations.BackgroundRunner.Handlers;

/// <summary>
/// Handles <see cref="AlterationPlanSubmitted"/> notifications and runs the plan in the background.
/// </summary>
public class AlterationPlanSubmittedHandler : INotificationHandler<AlterationPlanSubmitted>
{
    private readonly IJobQueue _jobQueue;
    private readonly IAlterationPlanStore _alterationPlanStore;
    private readonly IAlterationJobStore _alterationJobStore;
    private readonly IAlterationJobRunner _alterationJobRunner;

    /// <summary>
    /// Initializes a new instance of the <see cref="AlterationPlanSubmittedHandler"/> class.
    /// </summary>
    public AlterationPlanSubmittedHandler(IJobQueue jobQueue, IAlterationPlanStore alterationPlanStore, IAlterationJobStore alterationJobStore, IAlterationJobRunner alterationJobRunner)
    {
        _jobQueue = jobQueue;
        _alterationPlanStore = alterationPlanStore;
        _alterationJobStore = alterationJobStore;
        _alterationJobRunner = alterationJobRunner;
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
        var alterationJobs = await _alterationJobStore.FindManyAsync(filter, cancellationToken);

        // Enqueue each job.
        foreach (var alterationJob in alterationJobs)
        {
            _jobQueue.Enqueue(ct => ExecuteJobAsync(alterationJob.Id, ct));
        }
    }

    private async Task ExecuteJobAsync(string alterationJobId, CancellationToken cancellationToken)
    {
        await _alterationJobRunner.RunAsync(alterationJobId, cancellationToken);
    }
}