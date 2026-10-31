using Elsa.Common.Multitenancy;
using Elsa.Extensions;
using Elsa.Scheduling.Quartz.Contracts;
using Elsa.Scheduling.Quartz.Jobs;
using Elsa.Workflows.Runtime;
using Microsoft.Extensions.Logging;
using Quartz;
using QuartzIScheduler = Quartz.IScheduler;

namespace Elsa.Scheduling.Quartz.Services;

/// <summary>
/// Invokes activities from a background worker within the context of its workflow instance using Quartz.NET.
/// </summary>
public class QuartzBackgroundActivityScheduler(
    ISchedulerFactory schedulerFactory,
    ITenantAccessor tenantAccessor,
    ILogger<QuartzBackgroundActivityScheduler> logger) : IBackgroundActivityScheduler
{
    private readonly IJobKeyProvider _jobKeyProvider = new JobKeyProvider(tenantAccessor);

    /// <inheritdoc />
    public async Task<string> CreateAsync(ScheduledBackgroundActivity scheduledBackgroundActivity, CancellationToken cancellationToken = default)
    {
        var scheduler = await schedulerFactory.GetScheduler(cancellationToken);
        var jobId = Guid.NewGuid().ToString("N");
        var job = JobBuilder.Create<ExecuteBackgroundActivityJob>()
            .WithIdentity(GetJobKey(jobId))
            .UsingJobData(CreateJobDataMap(scheduledBackgroundActivity))
            .StoreDurably()
            .Build();

        await AddJobAsync(scheduler, job, cancellationToken);
        return jobId;
    }

    /// <inheritdoc />
    public async Task ScheduleAsync(string jobId, CancellationToken cancellationToken = default)
    {
        var scheduler = await schedulerFactory.GetScheduler(cancellationToken);
        var jobKey = GetJobKey(jobId);

        if (!await scheduler.CheckExists(jobKey, cancellationToken))
            return;

        var trigger = TriggerBuilder.Create()
            .ForJob(jobKey)
            .WithIdentity(GetTriggerKey(jobId))
            .StartNow()
            .Build();

        await ScheduleTriggerAsync(scheduler, trigger, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string> ScheduleAsync(ScheduledBackgroundActivity scheduledBackgroundActivity, CancellationToken cancellationToken = default)
    {
        var jobId = await CreateAsync(scheduledBackgroundActivity, cancellationToken);
        await ScheduleAsync(jobId, cancellationToken);
        return jobId;
    }

    /// <inheritdoc />
    public Task UnscheduledAsync(string jobId, CancellationToken cancellationToken = default) => DeleteJobAsync(jobId, cancellationToken);

    /// <inheritdoc />
    public Task CancelAsync(string jobId, CancellationToken cancellationToken = default) => DeleteJobAsync(jobId, cancellationToken);

    private async Task DeleteJobAsync(string jobId, CancellationToken cancellationToken)
    {
        var scheduler = await schedulerFactory.GetScheduler(cancellationToken);
        await scheduler.DeleteJob(GetJobKey(jobId), cancellationToken);
    }

    private async Task AddJobAsync(QuartzIScheduler scheduler, IJobDetail job, CancellationToken cancellationToken)
    {
        try
        {
            const bool replaceExisting = false;
            await scheduler.AddJob(job, replaceExisting, cancellationToken);
        }
        catch (JobPersistenceException e) when (e.InnerException is ObjectAlreadyExistsException)
        {
            logger.LogDebug("Job {JobKey} already exists, skipping registration. This is expected in clustered deployments during concurrent operations", job.Key);
        }
        catch (ObjectAlreadyExistsException)
        {
            logger.LogDebug("Job {JobKey} already exists, skipping registration. This is expected in clustered deployments during concurrent operations", job.Key);
        }
    }

    private async Task ScheduleTriggerAsync(QuartzIScheduler scheduler, ITrigger trigger, CancellationToken cancellationToken)
    {
        try
        {
            await scheduler.ScheduleJob(trigger, cancellationToken);
        }
        catch (JobPersistenceException e) when (e.InnerException is ObjectAlreadyExistsException)
        {
            logger.LogDebug("Trigger {TriggerKey} already exists (wrapped), skipping scheduling. This is expected in clustered deployments during concurrent operations", trigger.Key);
        }
        catch (ObjectAlreadyExistsException)
        {
            logger.LogDebug("Trigger {TriggerKey} already exists, skipping scheduling. This is expected in clustered deployments during concurrent operations", trigger.Key);
        }
    }

    private JobDataMap CreateJobDataMap(ScheduledBackgroundActivity scheduledBackgroundActivity)
    {
        return new JobDataMap()
            .AddIfNotEmpty("TenantId", tenantAccessor.Tenant?.Id)
            .AddIfNotEmpty(nameof(ScheduledBackgroundActivity.WorkflowInstanceId), scheduledBackgroundActivity.WorkflowInstanceId)
            .AddIfNotEmpty(nameof(ScheduledBackgroundActivity.ActivityNodeId), scheduledBackgroundActivity.ActivityNodeId)
            .AddIfNotEmpty(nameof(ScheduledBackgroundActivity.BookmarkId), scheduledBackgroundActivity.BookmarkId);
    }

    private JobKey GetJobKey(string jobId) => new(jobId, _jobKeyProvider.GetGroupName());
    private TriggerKey GetTriggerKey(string jobId) => new(jobId, _jobKeyProvider.GetGroupName());
}
