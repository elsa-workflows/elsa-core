using System.Diagnostics.CodeAnalysis;
using Elsa.Common.Contracts;
using Elsa.Extensions;
using Elsa.Quartz.Jobs;
using Elsa.Scheduling.Contracts;
using Elsa.Scheduling.Services;
using Elsa.Workflows.Runtime.Requests;
using Quartz;

namespace Elsa.Quartz.Services;

/// <summary>
/// An implementation of <see cref="Scheduling.Contracts.IWorkflowScheduler"/> that uses Quartz.NET.
/// </summary>
public class QuartzWorkflowScheduler(ISchedulerFactory schedulerFactoryFactory, IJsonSerializer jsonSerializer) : IWorkflowScheduler
{
    /// <inheritdoc />
    public async ValueTask ScheduleAtAsync(string taskName, DispatchWorkflowDefinitionRequest request, DateTimeOffset at, CancellationToken cancellationToken = default)
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
    public async ValueTask ScheduleAtAsync(string taskName, DispatchWorkflowInstanceRequest request, DateTimeOffset at, CancellationToken cancellationToken = default)
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
    public async ValueTask ScheduleRecurringAsync(string taskName, DispatchWorkflowDefinitionRequest request, DateTimeOffset startAt, TimeSpan interval, CancellationToken cancellationToken = default)
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
    public async ValueTask ScheduleRecurringAsync(string taskName, DispatchWorkflowInstanceRequest request, DateTimeOffset startAt, TimeSpan interval, CancellationToken cancellationToken = default)
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
    public async ValueTask ScheduleCronAsync(string taskName, DispatchWorkflowDefinitionRequest request, string cronExpression, CancellationToken cancellationToken = default)
    {
        var scheduler = await schedulerFactoryFactory.GetScheduler(cancellationToken);
        var trigger = TriggerBuilder.Create().ForJob(RunWorkflowJob.JobKey).UsingJobData(CreateJobDataMap(request)).WithIdentity(taskName).WithCronSchedule(cronExpression).Build();

        if (!await scheduler.CheckExists(trigger.Key, cancellationToken))
            await scheduler.ScheduleJob(trigger, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask ScheduleCronAsync(string taskName, DispatchWorkflowInstanceRequest request, string cronExpression, CancellationToken cancellationToken = default)
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

    private JobDataMap CreateJobDataMap(DispatchWorkflowDefinitionRequest request)
    {
        return new JobDataMap()
            .AddIfNotEmpty(nameof(DispatchWorkflowDefinitionRequest.InstanceId), request.InstanceId)
            .AddIfNotEmpty(nameof(DispatchWorkflowDefinitionRequest.CorrelationId), request.CorrelationId)
            .AddIfNotEmpty(nameof(DispatchWorkflowDefinitionRequest.DefinitionVersionId), request.DefinitionVersionId)
            .AddIfNotEmpty(nameof(DispatchWorkflowDefinitionRequest.TriggerActivityId), request.TriggerActivityId)
            .AddIfNotEmpty(nameof(DispatchWorkflowDefinitionRequest.Input), request.Input);
    }

    [RequiresUnreferencedCode("Calls Elsa.Common.Contracts.IJsonSerializer.Serialize(Object)")]
    private JobDataMap CreateJobDataMap(DispatchWorkflowInstanceRequest request)
    {
        var serializedActivityHandle = request.ActivityHandle != null ? jsonSerializer.Serialize(request.ActivityHandle) : null;
        
        return new JobDataMap()
            .AddIfNotEmpty(nameof(DispatchWorkflowInstanceRequest.InstanceId), request.InstanceId)
            .AddIfNotEmpty(nameof(DispatchWorkflowInstanceRequest.CorrelationId), request.CorrelationId)
            .AddIfNotEmpty(nameof(DispatchWorkflowInstanceRequest.Input), request.Input)
            .AddIfNotEmpty(nameof(DispatchWorkflowInstanceRequest.ActivityHandle), serializedActivityHandle)
            .AddIfNotEmpty(nameof(DispatchWorkflowInstanceRequest.BookmarkId), request.BookmarkId);
    }
}