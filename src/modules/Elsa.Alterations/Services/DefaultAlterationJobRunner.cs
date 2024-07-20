using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Entities;
using Elsa.Alterations.Core.Enums;
using Elsa.Alterations.Core.Filters;
using Elsa.Alterations.Core.Notifications;
using Elsa.Common.Contracts;
using Elsa.Mediator.Contracts;

namespace Elsa.Alterations.Services;

/// <inheritdoc />
/// <summary>
/// Initializes a new instance of the <see cref="DefaultAlterationJobRunner"/> class.
/// </summary>
public class DefaultAlterationJobRunner(
    IAlterationPlanStore alterationPlanStore,
    IAlterationJobStore alterationJobStore,
    IAlterationRunner alterationRunner,
    INotificationSender notificationSender,
    ISystemClock systemClock) : IAlterationJobRunner
{
    private readonly IAlterationPlanStore _alterationPlanStore = alterationPlanStore;
    private readonly IAlterationJobStore _alterationJobStore = alterationJobStore;
    private readonly IAlterationRunner _alterationRunner = alterationRunner;
    private readonly INotificationSender _notificationSender = notificationSender;
    private readonly ISystemClock _systemClock = systemClock;

    /// <inheritdoc />
    public async Task<AlterationJob> RunAsync(string jobId, CancellationToken cancellationToken = default)
    {
        var job = (await _alterationJobStore.FindAsync(new AlterationJobFilter { Id = jobId }, cancellationToken))!;
        var plan = (await _alterationPlanStore.FindAsync(new AlterationPlanFilter { Id = job.PlanId }, cancellationToken))!;
        var workflowInstanceId = job.WorkflowInstanceId;
        
        job.Status = AlterationJobStatus.Running;
        job.StartedAt = _systemClock.UtcNow;
        await _alterationJobStore.SaveAsync(job, cancellationToken);
        
        var result = await _alterationRunner.RunAsync(workflowInstanceId, plan.Alterations, cancellationToken);
        
        job.Status = result.IsSuccessful ? AlterationJobStatus.Completed : AlterationJobStatus.Failed;
        job.Log = result.Log.LogEntries.ToList();
        job.CompletedAt = _systemClock.UtcNow;
        await _alterationJobStore.SaveAsync(job, cancellationToken);
        await _notificationSender.SendAsync(new AlterationJobCompleted(job, result.WorkflowHasScheduledWork), cancellationToken);

        return job;
    }
}