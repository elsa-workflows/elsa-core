using Elsa.Common.Contracts;
using Elsa.Extensions;
using Elsa.Quartz.Jobs;
using Elsa.Scheduling;
using Quartz;

namespace Elsa.Quartz.Services;

/// <summary>
/// An implementation of <see cref="IWorkflowScheduler"/> that uses Quartz.NET.
/// </summary>
public class QuartzWorkflowScheduler(ISchedulerFactory schedulerFactoryFactory, IJsonSerializer jsonSerializer) : IWorkflowScheduler
{
    /// <inheritdoc />
    public async ValueTask ScheduleAtAsync(string taskName, ScheduleNewWorkflowInstanceRequest request, DateTimeOffset at, CancellationToken cancellationToken = default)
    {
        var scheduler = await schedulerFactoryFactory.GetScheduler(cancellationToken);
        var trigger = TriggerBuilder.Create()
            .ForJob(RunWorkflowJob.JobKey)
            .UsingJobData(CreateJobDataMap(request))
            .WithIdentity(taskName)
            .StartAt(at)
            .Build();

        if (!await scheduler.CheckExists(trigger.Key, cancellationToken))
            await scheduler.ScheduleJob(trigger, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask ScheduleAtAsync(string taskName, ScheduleExistingWorkflowInstanceRequest request, DateTimeOffset at, CancellationToken cancellationToken = default)
    {
        var scheduler = await schedulerFactoryFactory.GetScheduler(cancellationToken);
        var trigger = TriggerBuilder.Create()
            .ForJob(ResumeWorkflowJob.JobKey)
            .UsingJobData(CreateJobDataMap(request))
            .WithIdentity(taskName)
            .StartAt(at)
            .Build();
        
        if(!await scheduler.CheckExists(trigger.Key, cancellationToken))
            await scheduler.ScheduleJob(trigger, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask ScheduleRecurringAsync(string taskName, ScheduleNewWorkflowInstanceRequest request, DateTimeOffset startAt, TimeSpan interval, CancellationToken cancellationToken = default)
    {
        var scheduler = await schedulerFactoryFactory.GetScheduler(cancellationToken);
        var trigger = TriggerBuilder.Create()
            .ForJob(RunWorkflowJob.JobKey)
            .WithIdentity(taskName)
            .UsingJobData(CreateJobDataMap(request))
            .StartAt(startAt)
            .WithSimpleSchedule(schedule => schedule.WithInterval(interval).RepeatForever())
            .Build();

        if (!await scheduler.CheckExists(trigger.Key, cancellationToken))
            await scheduler.ScheduleJob(trigger, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask ScheduleRecurringAsync(string taskName, ScheduleExistingWorkflowInstanceRequest request, DateTimeOffset startAt, TimeSpan interval, CancellationToken cancellationToken = default)
    {
        var scheduler = await schedulerFactoryFactory.GetScheduler(cancellationToken);
        var trigger = TriggerBuilder.Create()
            .ForJob(ResumeWorkflowJob.JobKey)
            .WithIdentity(taskName)
            .UsingJobData(CreateJobDataMap(request))
            .StartAt(startAt)
            .WithSimpleSchedule(schedule => schedule.WithInterval(interval).RepeatForever())
            .Build();
        
        if(!await scheduler.CheckExists(trigger.Key, cancellationToken))
            await scheduler.ScheduleJob(trigger, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask ScheduleCronAsync(string taskName, ScheduleNewWorkflowInstanceRequest request, string cronExpression, CancellationToken cancellationToken = default)
    {
        var scheduler = await schedulerFactoryFactory.GetScheduler(cancellationToken);
        var trigger = TriggerBuilder.Create().ForJob(RunWorkflowJob.JobKey).UsingJobData(CreateJobDataMap(request)).WithIdentity(taskName).WithCronSchedule(cronExpression).Build();

        if (!await scheduler.CheckExists(trigger.Key, cancellationToken))
            await scheduler.ScheduleJob(trigger, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask ScheduleCronAsync(string taskName, ScheduleExistingWorkflowInstanceRequest request, string cronExpression, CancellationToken cancellationToken = default)
    {
        var scheduler = await schedulerFactoryFactory.GetScheduler(cancellationToken);
        var trigger = TriggerBuilder.Create()
            .ForJob(ResumeWorkflowJob.JobKey)
            .UsingJobData(CreateJobDataMap(request))
            .WithIdentity(taskName)
            .WithCronSchedule(cronExpression).Build();
        
        if(!await scheduler.CheckExists(trigger.Key, cancellationToken))
            await scheduler.ScheduleJob(trigger, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask UnscheduleAsync(string taskName, CancellationToken cancellationToken = default)
    {
        var scheduler = await schedulerFactoryFactory.GetScheduler(cancellationToken);
        var triggerKey = new TriggerKey(taskName);
        await scheduler.UnscheduleJob(triggerKey, cancellationToken);
    }

    private JobDataMap CreateJobDataMap(ScheduleNewWorkflowInstanceRequest request)
    {
        return new JobDataMap()
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
            .AddIfNotEmpty(nameof(ScheduleExistingWorkflowInstanceRequest.WorkflowInstanceId), request.WorkflowInstanceId)
            .AddIfNotEmpty(nameof(ScheduleExistingWorkflowInstanceRequest.Input), request.Input)
            .AddIfNotEmpty(nameof(ScheduleExistingWorkflowInstanceRequest.Properties), request.Properties)
            .AddIfNotEmpty(nameof(ScheduleExistingWorkflowInstanceRequest.ActivityHandle), serializedActivityHandle)
            .AddIfNotEmpty(nameof(ScheduleExistingWorkflowInstanceRequest.BookmarkId), request.BookmarkId);
    }
}