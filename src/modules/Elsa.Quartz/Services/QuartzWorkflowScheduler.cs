using Elsa.Extensions;
using Elsa.Quartz.Jobs;
using Elsa.Scheduling.Contracts;
using Elsa.Scheduling.Services;
using Elsa.Workflows.Runtime.Models.Requests;
using Quartz;

namespace Elsa.Quartz.Services;

/// <summary>
/// An implementation of <see cref="Scheduling.Contracts.IWorkflowScheduler"/> that uses Quartz.NET.
/// </summary>
public class QuartzWorkflowScheduler : IWorkflowScheduler
{
    private readonly ISchedulerFactory _schedulerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultWorkflowScheduler"/> class.
    /// </summary>
    public QuartzWorkflowScheduler(ISchedulerFactory schedulerFactoryFactory)
    {
        _schedulerFactory = schedulerFactoryFactory;
    }

    /// <inheritdoc />
    public async ValueTask ScheduleAtAsync(string taskName, DispatchWorkflowDefinitionRequest request, DateTimeOffset at, CancellationToken cancellationToken = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
        var trigger = TriggerBuilder.Create()
            .ForJob(RunWorkflowJob.JobKey)
            .UsingJobData(CreateJobDataMap(request))
            .WithIdentity(taskName)
            .StartAt(at)
            .Build();
        await scheduler.ScheduleJob(trigger, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask ScheduleAtAsync(string taskName, DispatchWorkflowInstanceRequest request, DateTimeOffset at, CancellationToken cancellationToken = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
        var trigger = TriggerBuilder.Create()
            .ForJob(ResumeWorkflowJob.JobKey)
            .UsingJobData(CreateJobDataMap(request))
            .WithIdentity(taskName)
            .StartAt(at)
            .Build();
        await scheduler.ScheduleJob(trigger, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask ScheduleRecurringAsync(string taskName, DispatchWorkflowDefinitionRequest request, DateTimeOffset startAt, TimeSpan interval, CancellationToken cancellationToken = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
        var trigger = TriggerBuilder.Create()
            .ForJob(RunWorkflowJob.JobKey)
            .WithIdentity(taskName)
            .UsingJobData(CreateJobDataMap(request))
            .StartAt(startAt)
            .WithSimpleSchedule(schedule => schedule.WithInterval(interval).RepeatForever())
            .Build();
        await scheduler.ScheduleJob(trigger, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask ScheduleRecurringAsync(string taskName, DispatchWorkflowInstanceRequest request, DateTimeOffset startAt, TimeSpan interval, CancellationToken cancellationToken = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
        var trigger = TriggerBuilder.Create()
            .ForJob(ResumeWorkflowJob.JobKey)
            .WithIdentity(taskName)
            .UsingJobData(CreateJobDataMap(request))
            .StartAt(startAt)
            .WithSimpleSchedule(schedule => schedule.WithInterval(interval).RepeatForever())
            .Build();
        await scheduler.ScheduleJob(trigger, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask ScheduleCronAsync(string taskName, DispatchWorkflowDefinitionRequest request, string cronExpression, CancellationToken cancellationToken = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
        var trigger = TriggerBuilder.Create().ForJob(RunWorkflowJob.JobKey).UsingJobData(CreateJobDataMap(request)).WithIdentity(taskName).WithCronSchedule(cronExpression).Build();
        await scheduler.ScheduleJob(trigger, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask ScheduleCronAsync(string taskName, DispatchWorkflowInstanceRequest request, string cronExpression, CancellationToken cancellationToken = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
        var trigger = TriggerBuilder.Create()
            .ForJob(ResumeWorkflowJob.JobKey)
            .UsingJobData(CreateJobDataMap(request))
            .WithIdentity(taskName)
            .WithCronSchedule(cronExpression).Build();
        await scheduler.ScheduleJob(trigger, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask UnscheduleAsync(string taskName, CancellationToken cancellationToken = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
        var triggerKey = new TriggerKey(taskName);
        await scheduler.UnscheduleJob(triggerKey, cancellationToken);
    }

    private static JobDataMap CreateJobDataMap(DispatchWorkflowDefinitionRequest request) =>
        new JobDataMap()
            .AddIfNotEmpty(nameof(DispatchWorkflowDefinitionRequest.InstanceId), request.InstanceId)
            .AddIfNotEmpty(nameof(DispatchWorkflowDefinitionRequest.CorrelationId), request.CorrelationId)
            .AddIfNotEmpty(nameof(DispatchWorkflowDefinitionRequest.DefinitionId), request.DefinitionId)
            .AddIfNotEmpty(nameof(DispatchWorkflowDefinitionRequest.VersionOptions), request.VersionOptions.ToString())
            .AddIfNotEmpty(nameof(DispatchWorkflowDefinitionRequest.TriggerActivityId), request.TriggerActivityId)
            .AddIfNotEmpty(nameof(DispatchWorkflowDefinitionRequest.Input), request.Input)
        ;

    private static JobDataMap CreateJobDataMap(DispatchWorkflowInstanceRequest request) =>
        new JobDataMap()
            .AddIfNotEmpty(nameof(DispatchWorkflowInstanceRequest.InstanceId), request.InstanceId)
            .AddIfNotEmpty(nameof(DispatchWorkflowInstanceRequest.CorrelationId), request.CorrelationId)
            .AddIfNotEmpty(nameof(DispatchWorkflowInstanceRequest.Input), request.Input)
            .AddIfNotEmpty(nameof(DispatchWorkflowInstanceRequest.ActivityInstanceId), request.ActivityInstanceId)
            .AddIfNotEmpty(nameof(DispatchWorkflowInstanceRequest.ActivityId), request.ActivityId)
            .AddIfNotEmpty(nameof(DispatchWorkflowInstanceRequest.ActivityHash), request.ActivityHash)
            .AddIfNotEmpty(nameof(DispatchWorkflowInstanceRequest.BookmarkId), request.BookmarkId)
            .AddIfNotEmpty(nameof(DispatchWorkflowInstanceRequest.ActivityNodeId), request.ActivityNodeId);
}