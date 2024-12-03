using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.Extensions;
using Elsa.Quartz.Jobs;
using Elsa.Scheduling;
using Quartz;

namespace Elsa.Quartz.Services;

/// <summary>
/// An implementation of <see cref="IWorkflowScheduler"/> that uses Quartz.NET.
/// </summary>
public class QuartzWorkflowScheduler(ISchedulerFactory schedulerFactoryFactory, IJsonSerializer jsonSerializer, ITenantAccessor tenantAccessor) : IWorkflowScheduler
{
    /// <inheritdoc />
    public async ValueTask ScheduleAtAsync(string taskName, ScheduleNewWorkflowInstanceRequest request, DateTimeOffset at, CancellationToken cancellationToken = default)
    {
        var scheduler = await schedulerFactoryFactory.GetScheduler(cancellationToken);
        var job = JobBuilder.Create<RunWorkflowJob>()
            .WithIdentity(GetRunWorkflowJobKey())
            .Build();
        var trigger = TriggerBuilder.Create()
            .UsingJobData(CreateJobDataMap(request))
            .WithIdentity(GetTriggerKey(taskName))
            .StartAt(at)
            .Build();

        if (!await scheduler.CheckExists(job.Key, cancellationToken))
            await scheduler.ScheduleJob(job, trigger, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask ScheduleAtAsync(string taskName, ScheduleExistingWorkflowInstanceRequest request, DateTimeOffset at, CancellationToken cancellationToken = default)
    {
        var scheduler = await schedulerFactoryFactory.GetScheduler(cancellationToken);
        var job = JobBuilder.Create<ResumeWorkflowJob>().WithIdentity(GetResumeWorkflowJobKey()).Build();
        var trigger = TriggerBuilder.Create()
            .UsingJobData(CreateJobDataMap(request))
            .WithIdentity(GetTriggerKey(taskName))
            .StartAt(at)
            .Build();

        if (!await scheduler.CheckExists(job.Key, cancellationToken))
            await scheduler.ScheduleJob(job, trigger, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask ScheduleRecurringAsync(string taskName, ScheduleNewWorkflowInstanceRequest request, DateTimeOffset startAt, TimeSpan interval, CancellationToken cancellationToken = default)
    {
        var scheduler = await schedulerFactoryFactory.GetScheduler(cancellationToken);
        var job = JobBuilder.Create<RunWorkflowJob>().WithIdentity(GetRunWorkflowJobKey()).Build();
        var trigger = TriggerBuilder.Create()
            .WithIdentity(GetTriggerKey(taskName))
            .UsingJobData(CreateJobDataMap(request))
            .StartAt(startAt)
            .WithSimpleSchedule(schedule => schedule.WithInterval(interval).RepeatForever())
            .Build();

        if (!await scheduler.CheckExists(job.Key, cancellationToken))
            await scheduler.ScheduleJob(job, trigger, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask ScheduleRecurringAsync(string taskName, ScheduleExistingWorkflowInstanceRequest request, DateTimeOffset startAt, TimeSpan interval, CancellationToken cancellationToken = default)
    {
        var scheduler = await schedulerFactoryFactory.GetScheduler(cancellationToken);
        var job = JobBuilder.Create<ResumeWorkflowJob>().WithIdentity(GetResumeWorkflowJobKey()).Build();
        var trigger = TriggerBuilder.Create()
            .WithIdentity(GetTriggerKey(taskName))
            .UsingJobData(CreateJobDataMap(request))
            .StartAt(startAt)
            .WithSimpleSchedule(schedule => schedule.WithInterval(interval).RepeatForever())
            .Build();

        if (!await scheduler.CheckExists(job.Key, cancellationToken))
            await scheduler.ScheduleJob(job, trigger, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask ScheduleCronAsync(string taskName, ScheduleNewWorkflowInstanceRequest request, string cronExpression, CancellationToken cancellationToken = default)
    {
        var scheduler = await schedulerFactoryFactory.GetScheduler(cancellationToken);
        var job = JobBuilder.Create<RunWorkflowJob>().WithIdentity(GetRunWorkflowJobKey()).Build();
        var trigger = TriggerBuilder.Create()
            .UsingJobData(CreateJobDataMap(request))
            .WithIdentity(GetTriggerKey(taskName))
            .WithCronSchedule(cronExpression)
            .Build();

        if (!await scheduler.CheckExists(job.Key, cancellationToken))
            await scheduler.ScheduleJob(job, trigger, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask ScheduleCronAsync(string taskName, ScheduleExistingWorkflowInstanceRequest request, string cronExpression, CancellationToken cancellationToken = default)
    {
        var scheduler = await schedulerFactoryFactory.GetScheduler(cancellationToken);
        var job = JobBuilder.Create<ResumeWorkflowJob>().WithIdentity(GetResumeWorkflowJobKey()).Build();
        var trigger = TriggerBuilder.Create()
            .UsingJobData(CreateJobDataMap(request))
            .WithIdentity(GetTriggerKey(taskName))
            .WithCronSchedule(cronExpression).Build();

        if (!await scheduler.CheckExists(job.Key, cancellationToken))
            await scheduler.ScheduleJob(job, trigger, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask UnscheduleAsync(string taskName, CancellationToken cancellationToken = default)
    {
        var scheduler = await schedulerFactoryFactory.GetScheduler(cancellationToken);
        var triggerKey = GetTriggerKey(taskName);
        await scheduler.UnscheduleJob(triggerKey, cancellationToken);
    }

    private JobDataMap CreateJobDataMap(ScheduleNewWorkflowInstanceRequest request)
    {
        return new JobDataMap()
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
            .AddIfNotEmpty("TenantId", tenantAccessor.Tenant?.Id)
            .AddIfNotEmpty(nameof(ScheduleExistingWorkflowInstanceRequest.WorkflowInstanceId), request.WorkflowInstanceId)
            .AddIfNotEmpty(nameof(ScheduleExistingWorkflowInstanceRequest.Input), request.Input)
            .AddIfNotEmpty(nameof(ScheduleExistingWorkflowInstanceRequest.Properties), request.Properties)
            .AddIfNotEmpty(nameof(ScheduleExistingWorkflowInstanceRequest.ActivityHandle), serializedActivityHandle)
            .AddIfNotEmpty(nameof(ScheduleExistingWorkflowInstanceRequest.BookmarkId), request.BookmarkId);
    }
    
    private JobKey GetRunWorkflowJobKey() => new(nameof(RunWorkflowJob),  GetGroupName());
    private JobKey GetResumeWorkflowJobKey() => new(nameof(ResumeWorkflowJob), GetGroupName());

    private string GetGroupName()
    {
        var tenantId = tenantAccessor.Tenant?.Id;
        return string.IsNullOrWhiteSpace(tenantId) ? "Default" : tenantId;
    }
    
    private TriggerKey GetTriggerKey(string taskName)
    {
        return new TriggerKey(taskName, GetGroupName());
    }
}