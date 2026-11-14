using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Entities;
using Elsa.Alterations.Core.Enums;
using Elsa.Alterations.Core.Filters;
using Elsa.Alterations.Core.Models;
using Elsa.Alterations.Core.Notifications;
using Elsa.Alterations.Core.Results;
using Elsa.Common;
using Elsa.Mediator.Contracts;
using Microsoft.Extensions.Logging;

namespace Elsa.Alterations.Services;

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
        
        job.Status = AlterationJobStatus.Running;
        job.StartedAt = _systemClock.UtcNow;
        await _alterationJobStore.SaveAsync(job, cancellationToken);
        
        RunAlterationsResult result;

        try
        {
            result = await _alterationRunner.RunAsync(workflowInstanceId, plan.Alterations, cancellationToken);
        }
        catch (Exception e) when (!cancellationToken.IsCancellationRequested)
        {
            // Don't leave the job in the Running state: record the failure so the plan can complete.
            // Once the job's token is cancelled nothing is recorded and the exception propagates.
            job.Status = AlterationJobStatus.Failed;
            job.Log = [new AlterationLogEntry(e.Message, LogLevel.Error, _systemClock.UtcNow, "Exception")];
            job.CompletedAt = _systemClock.UtcNow;
            await _alterationJobStore.SaveAsync(job, cancellationToken);
            await _notificationSender.SendAsync(new AlterationJobCompleted(job, false), cancellationToken);
            throw;
        }

        job.Status = result.IsSuccessful ? AlterationJobStatus.Completed : AlterationJobStatus.Failed;
        job.Log = result.Log.LogEntries.ToList();
        job.CompletedAt = _systemClock.UtcNow;
        await _alterationJobStore.SaveAsync(job, cancellationToken);
        await _notificationSender.SendAsync(new AlterationJobCompleted(job, result.WorkflowHasScheduledWork), cancellationToken);

        return job;
    }
}