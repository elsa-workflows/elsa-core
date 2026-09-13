using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.Extensions;
using Elsa.Scheduling.Quartz.Contracts;
using Elsa.Scheduling.Quartz.Jobs;
using Microsoft.Extensions.Logging;
using Quartz;
using QuartzIScheduler = Quartz.IScheduler;

namespace Elsa.Scheduling.Quartz.Services;

/// <summary>
/// An implementation of <see cref="IWorkflowScheduler"/> that uses Quartz.NET.
/// </summary>
public class QuartzWorkflowScheduler(
    ISchedulerFactory schedulerFactoryFactory,
    IJsonSerializer jsonSerializer,
    ITenantAccessor tenantAccessor,
    IJobKeyProvider jobKeyProvider,
    ILogger<QuartzWorkflowScheduler> logger,
    IQuartzScheduleCoordinator? scheduleCoordinator = null) : IWorkflowScheduler
{
    /// <inheritdoc />
    public async ValueTask ScheduleAtAsync(string taskName, ScheduleNewWorkflowInstanceRequest request, DateTimeOffset at, CancellationToken cancellationToken = default)
    {
        var scheduler = await schedulerFactoryFactory.GetScheduler(cancellationToken);

        var trigger = TriggerBuilder.Create()
            .ForJob(GetRunWorkflowJobKey())
            .UsingJobData(CreateJobDataMap(request))
            .WithIdentity(GetTriggerKey(taskName))
            .StartAt(at)
            .Build();

        await ScheduleJobAsync<RunWorkflowJob>(scheduler, trigger, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask ScheduleAtAsync(string taskName, ScheduleExistingWorkflowInstanceRequest request, DateTimeOffset at, CancellationToken cancellationToken = default)
    {
        var scheduler = await schedulerFactoryFactory.GetScheduler(cancellationToken);
        var trigger = TriggerBuilder.Create()
            .ForJob(GetResumeWorkflowJobKey())
            .UsingJobData(CreateJobDataMap(request))
            .WithIdentity(GetTriggerKey(taskName))
            .StartAt(at)
            .Build();

        await ScheduleJobAsync<ResumeWorkflowJob>(scheduler, trigger, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask ScheduleRecurringAsync(string taskName, ScheduleNewWorkflowInstanceRequest request, DateTimeOffset startAt, TimeSpan interval, CancellationToken cancellationToken = default)
    {
        var scheduler = await schedulerFactoryFactory.GetScheduler(cancellationToken);
        var trigger = TriggerBuilder.Create()
            .WithIdentity(GetTriggerKey(taskName))
            .ForJob(GetRunWorkflowJobKey())
            .UsingJobData(CreateJobDataMap(request))
            .StartAt(startAt)
            .WithSimpleSchedule(schedule => schedule.WithInterval(interval).RepeatForever())
            .Build();

        await ScheduleJobAsync<RunWorkflowJob>(scheduler, trigger, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask ScheduleRecurringAsync(string taskName, ScheduleExistingWorkflowInstanceRequest request, DateTimeOffset startAt, TimeSpan interval, CancellationToken cancellationToken = default)
    {
        var scheduler = await schedulerFactoryFactory.GetScheduler(cancellationToken);
        var trigger = TriggerBuilder.Create()
            .WithIdentity(GetTriggerKey(taskName))
            .ForJob(GetResumeWorkflowJobKey())
            .UsingJobData(CreateJobDataMap(request))
            .StartAt(startAt)
            .WithSimpleSchedule(schedule => schedule.WithInterval(interval).RepeatForever())
            .Build();

        await ScheduleJobAsync<ResumeWorkflowJob>(scheduler, trigger, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask ScheduleCronAsync(string taskName, ScheduleNewWorkflowInstanceRequest request, string cronExpression, CancellationToken cancellationToken = default)
    {
        var scheduler = await schedulerFactoryFactory.GetScheduler(cancellationToken);
        var trigger = TriggerBuilder.Create()
            .UsingJobData(CreateJobDataMap(request))
            .ForJob(GetRunWorkflowJobKey())
            .WithIdentity(GetTriggerKey(taskName))
            .WithCronSchedule(cronExpression)
            .Build();

        await ScheduleJobAsync<RunWorkflowJob>(scheduler, trigger, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask ScheduleCronAsync(string taskName, ScheduleExistingWorkflowInstanceRequest request, string cronExpression, CancellationToken cancellationToken = default)
    {
        var scheduler = await schedulerFactoryFactory.GetScheduler(cancellationToken);
        var trigger = TriggerBuilder.Create()
            .ForJob(GetResumeWorkflowJobKey())
            .UsingJobData(CreateJobDataMap(request))
            .WithIdentity(GetTriggerKey(taskName))
            .WithCronSchedule(cronExpression).Build();

        await ScheduleJobAsync<ResumeWorkflowJob>(scheduler, trigger, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask UnscheduleAsync(string taskName, CancellationToken cancellationToken = default)
    {
        var scheduler = await schedulerFactoryFactory.GetScheduler(cancellationToken);
        var triggerKey = GetTriggerKey(taskName);
        await ExecuteCoordinatedAsync(triggerKey, async token =>
        {
            await scheduler.UnscheduleJob(triggerKey, token);
            await scheduler.UnscheduleJob(QuartzTriggerKeys.GetRetryTriggerKey(triggerKey), token);
        }, cancellationToken);
    }
    
    private async Task ScheduleJobAsync<TJobType>(QuartzIScheduler scheduler, ITrigger trigger, CancellationToken cancellationToken) where TJobType : IJob
    {
        await ExecuteCoordinatedAsync(QuartzTriggerKeys.GetOriginalTriggerKey(trigger), async token =>
        {
            // Ensure the durable job referenced by the trigger exists before scheduling.
            // The job is normally registered at startup by RegisterJobsTask, but it can be absent here when:
            // - the trigger targets a tenant-specific job group that was never registered at startup,
            // - the startup task has not run yet (or was skipped), or
            // - the job rows were removed from the Quartz job store at runtime.
            // Without this, Quartz throws "The job (...RunWorkflowJob) referenced by the trigger does not exist".
            await EnsureJobAsync<TJobType>(scheduler, trigger.JobKey, token);

            try
            {
                // Try to schedule the trigger. In clustered mode, multiple instances may attempt this simultaneously.
                // The ScheduleJob method will throw ObjectAlreadyExistsException if a trigger with the same key already exists.
                // Unlike AddJob, ScheduleJob does not have a 'replace' parameter - it always fails if the trigger exists.
                // Note: To update an existing trigger, callers should first use UnscheduleAsync before scheduling the new trigger.
                await scheduler.ScheduleJob(trigger, token);
            }
            catch (JobPersistenceException e) when (e.InnerException is ObjectAlreadyExistsException)
            {
                // SQL-backed Quartz stores (AdoJobStore) wrap the duplicate-trigger error in a JobPersistenceException.
                // In clustered mode, this is an expected race condition when multiple pods attempt to schedule the same trigger.
                logger.LogDebug("Trigger {TriggerKey} already exists (wrapped), skipping scheduling. This is expected in clustered deployments during concurrent operations", trigger.Key);
            }
            catch (ObjectAlreadyExistsException)
            {
                // Trigger already exists. In clustered scenarios, this is an expected race condition
                // when multiple instances attempt to schedule the same trigger during tenant activation or startup.
                // We can safely ignore this and continue, as the trigger is already scheduled.
                logger.LogDebug("Trigger {TriggerKey} already exists, skipping scheduling. This is expected in clustered deployments during concurrent operations", trigger.Key);
            }
        }, cancellationToken);
    }

    private Task ExecuteCoordinatedAsync(TriggerKey originalTriggerKey, Func<CancellationToken, Task> action, CancellationToken cancellationToken) =>
        scheduleCoordinator?.ExecuteAsync(originalTriggerKey, action, cancellationToken) ?? action(cancellationToken);

    private async Task EnsureJobAsync<TJobType>(QuartzIScheduler scheduler, JobKey jobKey, CancellationToken cancellationToken) where TJobType : IJob
    {
        // Fast path: the durable job is already registered.
        if (await scheduler.CheckExists(jobKey, cancellationToken))
            return;

        var job = JobBuilder.Create<TJobType>()
            .WithIdentity(jobKey)
            .StoreDurably()
            .Build();

        try
        {
            // Use replace=false so we don't overwrite an existing job definition. In clustered mode,
            // multiple instances may attempt this simultaneously between the CheckExists call and here.
            const bool replaceExisting = false;
            await scheduler.AddJob(job, replaceExisting, cancellationToken);
        }
        catch (JobPersistenceException e) when (e.InnerException is ObjectAlreadyExistsException)
        {
            // Job already exists, which is fine: another instance registered it concurrently.
            logger.LogDebug("Job {JobKey} already exists, skipping registration. This is expected in clustered deployments during concurrent operations", jobKey);
        }
        catch (ObjectAlreadyExistsException)
        {
            // Job already exists, which is fine: another instance registered it concurrently.
            logger.LogDebug("Job {JobKey} already exists, skipping registration. This is expected in clustered deployments during concurrent operations", jobKey);
        }
    }

    private JobDataMap CreateJobDataMap(ScheduleNewWorkflowInstanceRequest request)
    {
        return new JobDataMap()
                .AddIfNotEmpty(QuartzJobDataKeys.RetryScheduleGeneration, Guid.NewGuid().ToString("N"))
                .AddIfNotEmpty("TenantId", tenantAccessor.Tenant?.Id)
                .AddIfNotEmpty(nameof(ScheduleNewWorkflowInstanceRequest.CorrelationId), request.CorrelationId)
                .AddIfNotEmpty(nameof(ScheduleNewWorkflowInstanceRequest.WorkflowDefinitionHandle.DefinitionVersionId), request.WorkflowDefinitionHandle.DefinitionVersionId)
                .AddIfNotEmpty(nameof(ScheduleNewWorkflowInstanceRequest.TriggerActivityId), request.TriggerActivityId)
                .AddIfNotEmpty(nameof(ScheduleNewWorkflowInstanceRequest.ParentId), request.ParentId)
                .AddIfNotEmpty(nameof(ScheduleNewWorkflowInstanceRequest.Input), request.Input)
                .AddIfNotEmpty(nameof(ScheduleNewWorkflowInstanceRequest.Properties), request.Properties)
            ;
    }

    private JobDataMap CreateJobDataMap(ScheduleExistingWorkflowInstanceRequest request)
    {
        var serializedActivityHandle = request.ActivityHandle != null ? jsonSerializer.Serialize(request.ActivityHandle) : null;

        return new JobDataMap()
            .AddIfNotEmpty(QuartzJobDataKeys.RetryScheduleGeneration, Guid.NewGuid().ToString("N"))
            .AddIfNotEmpty("TenantId", tenantAccessor.Tenant?.Id)
            .AddIfNotEmpty(nameof(ScheduleExistingWorkflowInstanceRequest.WorkflowInstanceId), request.WorkflowInstanceId)
            .AddIfNotEmpty(nameof(ScheduleExistingWorkflowInstanceRequest.Input), request.Input)
            .AddIfNotEmpty(nameof(ScheduleExistingWorkflowInstanceRequest.Properties), request.Properties)
            .AddIfNotEmpty(nameof(ScheduleExistingWorkflowInstanceRequest.ActivityHandle), serializedActivityHandle)
            .AddIfNotEmpty(nameof(ScheduleExistingWorkflowInstanceRequest.BookmarkId), request.BookmarkId);
    }

    private JobKey GetRunWorkflowJobKey() => jobKeyProvider.GetJobKey<RunWorkflowJob>();
    private JobKey GetResumeWorkflowJobKey() => jobKeyProvider.GetJobKey<ResumeWorkflowJob>();
    private string GetGroupName() => jobKeyProvider.GetGroupName();
    private TriggerKey GetTriggerKey(string taskName) => new(taskName, GetGroupName());
}
