using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Entities;
using Elsa.Alterations.Core.Enums;
using Elsa.Alterations.Core.Filters;
using Elsa.Alterations.Core.Notifications;
using Elsa.Common.Contracts;
using Elsa.Mediator.Contracts;

namespace Elsa.Alterations.Core.Services;

/// <inheritdoc />
public class DefaultAlterationJobRunner : IAlterationJobRunner
{
    private readonly IAlterationPlanStore _alterationPlanStore;
    private readonly IAlterationJobStore _alterationJobStore;
    private readonly IAlterationRunner _alterationRunner;
    private readonly INotificationSender _notificationSender;
    private readonly ISystemClock _systemClock;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultAlterationJobRunner"/> class.
    /// </summary>
    public DefaultAlterationJobRunner(
        IAlterationPlanStore alterationPlanStore,
        IAlterationJobStore alterationJobStore,
        IAlterationRunner alterationRunner,
        INotificationSender notificationSender,
        ISystemClock systemClock)
    {
        _alterationPlanStore = alterationPlanStore;
        _alterationJobStore = alterationJobStore;
        _alterationRunner = alterationRunner;
        _notificationSender = notificationSender;
        _systemClock = systemClock;
    }

    /// <inheritdoc />
    public async Task<AlterationJob> RunAsync(string jobId, CancellationToken cancellationToken = default)
    {
        var job = (await _alterationJobStore.FindAsync(new AlterationJobFilter { Id = jobId }, cancellationToken))!;
        var plan = (await _alterationPlanStore.FindAsync(new AlterationPlanFilter { Id = job.PlanId }, cancellationToken))!;
        var workflowInstanceId = job.WorkflowInstanceId;
        var result = await _alterationRunner.RunAsync(workflowInstanceId, plan.Alterations, cancellationToken);
        
        job.Status = result.IsSuccessful ? AlterationJobStatus.Completed : AlterationJobStatus.Failed;
        job.Log = result.Log.LogEntries.ToList();
        job.CompletedAt = _systemClock.UtcNow;
        await _alterationJobStore.SaveAsync(job, cancellationToken);
        await _notificationSender.SendAsync(new AlterationJobCompleted(job, result.WorkflowHasScheduledWork), cancellationToken);

        return job;
    }
}