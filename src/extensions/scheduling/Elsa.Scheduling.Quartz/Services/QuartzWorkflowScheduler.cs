using System.Runtime.ExceptionServices;
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
            // Keep cancellation observable after Quartz removes a firing trigger. An acquired execution can fail
            // after both the original and its pending retry have disappeared, so the durable marker is written before
            // those keyed removals. An absent allowed-generation value denies every acquired generation.
            await PersistCancellationMarkerAsync(scheduler, triggerKey, allowedGeneration: null, token);

            // The marker is the cancellation barrier. Once it is durable, finish removing both keyed triggers even if
            // the caller cancels; Quartz has no transaction spanning the marker write and these removals, so honoring
            // cancellation here would leave a denied marker beside a still-live original trigger.
            await UnscheduleTriggersAsync(scheduler, triggerKey);

            // Quartz does not expose one transaction spanning the marker and trigger operations, so a process crash
            // can still leave a durable marker beside a trigger. A later keyed unschedule reconciles that state.
        }, cancellationToken);
    }
    
    private async Task ScheduleJobAsync<TJobType>(QuartzIScheduler scheduler, ITrigger trigger, CancellationToken cancellationToken) where TJobType : IJob
    {
        await ExecuteCoordinatedAsync(QuartzTriggerKeys.GetOriginalTriggerKey(trigger), async token =>
        {
            // A cancellation marker is retained across reschedules. Update it before persisting the replacement so an
            // interrupted schedule operation cannot allow an acquired older generation to recreate a retry. The marker
            // is keyed and therefore this remains constant-time per original schedule.
            var cancellationMarkerKey = QuartzTriggerKeys.GetCancellationMarkerJobKey(trigger.Key);
            var cancellationMarkerExists = await scheduler.GetJobDetail(cancellationMarkerKey, token) != null;
            var existingTrigger = cancellationMarkerExists ? await scheduler.GetTrigger(trigger.Key, token) : null;

            if (cancellationMarkerExists)
            {
                // Persist the proposed generation before scheduling, even when a preflight trigger snapshot exists:
                // Quartz can naturally remove that trigger while this operation waits, after which ScheduleJob may
                // successfully persist the proposed generation.
                await PersistCancellationMarkerAsync(scheduler, trigger.Key, QuartzTriggerKeys.GetScheduleGeneration(trigger), token);
            }

            // Once marker state is durable, caller cancellation must not interrupt the matching trigger mutation. This
            // preserves the marker-before-schedule invariant; scheduler acquisition and lock acquisition still use the
            // caller token above.
            var scheduleMutationToken = cancellationMarkerExists ? CancellationToken.None : token;

            try
            {
                // Ensure the durable job referenced by the trigger exists before scheduling.
                // The job is normally registered at startup by RegisterJobsTask, but it can be absent here when:
                // - the trigger targets a tenant-specific job group that was never registered at startup,
                // - the startup task has not run yet (or was skipped), or
                // - the job rows were removed from the Quartz job store at runtime.
                // Without this, Quartz throws "The job (...RunWorkflowJob) referenced by the trigger does not exist".
                await EnsureJobAsync<TJobType>(scheduler, trigger.JobKey, scheduleMutationToken);

                // Try to schedule the trigger. In clustered mode, multiple instances may attempt this simultaneously.
                // The ScheduleJob method will throw ObjectAlreadyExistsException if a trigger with the same key already exists.
                // Unlike AddJob, ScheduleJob does not have a 'replace' parameter - it always fails if the trigger exists.
                // Note: To update an existing trigger, callers should first use UnscheduleAsync before scheduling the new trigger.
                await scheduler.ScheduleJob(trigger, scheduleMutationToken);
            }
            catch (JobPersistenceException e) when (e.InnerException is ObjectAlreadyExistsException)
            {
                // SQL-backed Quartz stores (AdoJobStore) wrap the duplicate-trigger error in a JobPersistenceException.
                // In clustered mode, this is an expected race condition when multiple pods attempt to schedule the same trigger.
                if (cancellationMarkerExists)
                    await RestoreCancellationMarkerToStoredTriggerAsync(scheduler, trigger.Key, existingTrigger, scheduleMutationToken);

                logger.LogDebug("Trigger {TriggerKey} already exists (wrapped), skipping scheduling. This is expected in clustered deployments during concurrent operations", trigger.Key);
            }
            catch (ObjectAlreadyExistsException)
            {
                // Trigger already exists. In clustered scenarios, this is an expected race condition
                // when multiple instances attempt to schedule the same trigger during tenant activation or startup.
                // We can safely ignore this and continue, as the trigger is already scheduled.
                if (cancellationMarkerExists)
                    await RestoreCancellationMarkerToStoredTriggerAsync(scheduler, trigger.Key, existingTrigger, scheduleMutationToken);

                logger.LogDebug("Trigger {TriggerKey} already exists, skipping scheduling. This is expected in clustered deployments during concurrent operations", trigger.Key);
            }

        }, cancellationToken);
    }

    private static async Task UnscheduleTriggersAsync(QuartzIScheduler scheduler, TriggerKey originalTriggerKey)
    {
        var retryTriggerKey = QuartzTriggerKeys.GetRetryTriggerKey(originalTriggerKey);
        Exception? firstException = null;

        try
        {
            await scheduler.UnscheduleJob(originalTriggerKey, CancellationToken.None);
        }
        catch (Exception exception) when (exception is SchedulerException or OperationCanceledException)
        {
            firstException = exception;
        }

        try
        {
            await scheduler.UnscheduleJob(retryTriggerKey, CancellationToken.None);
        }
        catch (Exception exception) when (exception is SchedulerException or OperationCanceledException)
        {
            if (firstException != null)
                throw new AggregateException("Failed to unschedule the original and retry triggers.", firstException, exception);

            firstException = exception;
        }

        if (firstException != null)
            ExceptionDispatchInfo.Capture(firstException).Throw();
    }

    private static async Task RestoreCancellationMarkerToStoredTriggerAsync(QuartzIScheduler scheduler, TriggerKey originalTriggerKey, ITrigger? existingTrigger, CancellationToken cancellationToken)
    {
        // Prefer the post-exception store read because the preflight snapshot may have fired or been replaced while
        // ScheduleJob was in progress. If Quartz no longer exposes a trigger, the verified snapshot is the only known
        // duplicate target and keeps the marker from authorizing the unpersisted proposed generation.
        var storedTrigger = await scheduler.GetTrigger(originalTriggerKey, cancellationToken) ?? existingTrigger;

        if (storedTrigger != null)
            await PersistCancellationMarkerAsync(scheduler, originalTriggerKey, QuartzTriggerKeys.GetScheduleGeneration(storedTrigger), cancellationToken);
    }

    private static Task PersistCancellationMarkerAsync(QuartzIScheduler scheduler, TriggerKey originalTriggerKey, string? allowedGeneration, CancellationToken cancellationToken)
    {
        var markerData = new JobDataMap();
        if (allowedGeneration != null)
            markerData[QuartzJobDataKeys.CancellationAllowedScheduleGeneration] = allowedGeneration;

        var marker = JobBuilder.Create<QuartzScheduleCancellationJob>()
            .WithIdentity(QuartzTriggerKeys.GetCancellationMarkerJobKey(originalTriggerKey))
            .UsingJobData(markerData)
            .StoreDurably()
            .Build();

        return scheduler.AddJob(marker, replace: true, cancellationToken);
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
