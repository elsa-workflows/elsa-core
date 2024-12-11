using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.Extensions;
using Elsa.Quartz.Contracts;
using Elsa.Quartz.Jobs;
using Elsa.Scheduling;
using Quartz;
using IScheduler = Quartz.IScheduler;

namespace Elsa.Quartz.Services;

/// <summary>
/// An implementation of <see cref="IWorkflowScheduler"/> that uses Quartz.NET.
/// </summary>
internal class QuartzWorkflowScheduler(ISchedulerFactory schedulerFactoryFactory, IJsonSerializer jsonSerializer, ITenantAccessor tenantAccessor, IJobKeyProvider jobKeyProvider) : IWorkflowScheduler
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

        await ScheduleJobAsync(scheduler, trigger, cancellationToken);
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

        await ScheduleJobAsync(scheduler, trigger, cancellationToken);
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

        await ScheduleJobAsync(scheduler, trigger, cancellationToken);
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

        await ScheduleJobAsync(scheduler, trigger, cancellationToken);
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

        await ScheduleJobAsync(scheduler, trigger, cancellationToken);
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

        await ScheduleJobAsync(scheduler, trigger, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask UnscheduleAsync(string taskName, CancellationToken cancellationToken = default)
    {
        var scheduler = await schedulerFactoryFactory.GetScheduler(cancellationToken);
        var triggerKey = GetTriggerKey(taskName);
        await scheduler.UnscheduleJob(triggerKey, cancellationToken);
    }
    
    private async Task ScheduleJobAsync(IScheduler scheduler, ITrigger trigger, CancellationToken cancellationToken)
    {
        if (!await scheduler.CheckExists(trigger.Key, cancellationToken))
            await scheduler.ScheduleJob(trigger, cancellationToken);
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

    private JobKey GetRunWorkflowJobKey() => jobKeyProvider.GetJobKey<RunWorkflowJob>();
    private JobKey GetResumeWorkflowJobKey() => jobKeyProvider.GetJobKey<ResumeWorkflowJob>();
    private string GetGroupName() => jobKeyProvider.GetGroupName();
    private TriggerKey GetTriggerKey(string taskName) => new(taskName, GetGroupName());
}