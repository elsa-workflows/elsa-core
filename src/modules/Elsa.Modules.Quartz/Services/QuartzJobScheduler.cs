using Elsa.Jobs.Contracts;
using Elsa.Jobs.Schedules;
using Elsa.Modules.Quartz.Contracts;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl.Matchers;
using IElsaJobScheduler = Elsa.Jobs.Contracts.IJobScheduler;
using IElsaSchedule = Elsa.Jobs.Contracts.ISchedule;
using IJob = Elsa.Jobs.Contracts.IJob;

namespace Elsa.Modules.Quartz.Services;

public class QuartzJobScheduler : IJobScheduler
{
    public const string JobDataKey = "ElsaJob";
    public const string RootGroupKey = "ElsaJobs";
    private readonly IElsaJobSerializer _elsaJobSerializer;
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly ILogger _logger;

    public QuartzJobScheduler(IElsaJobSerializer elsaJobSerializer, ISchedulerFactory schedulerFactory, ILogger<QuartzJobScheduler> logger)
    {
        _elsaJobSerializer = elsaJobSerializer;
        _schedulerFactory = schedulerFactory;
        _logger = logger;
    }

    public async Task ScheduleAsync(IJob job, IElsaSchedule schedule, string[]? groupKeys, CancellationToken cancellationToken = default)
    {
        var quartzTrigger = CreateTrigger(job, schedule, groupKeys);
        await ScheduleJob(quartzTrigger, cancellationToken);
    }

    public async Task UnscheduleAsync(IJob job, CancellationToken cancellationToken = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
        var triggerKey = new TriggerKey(job.JobId);
        await scheduler.UnscheduleJob(triggerKey, cancellationToken);
    }

    public async Task ClearAsync(string[]? groupKeys = default, CancellationToken cancellationToken = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
        var groupKey = BuildGroupKey(groupKeys);
        var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupStartsWith(groupKey), cancellationToken);
        await scheduler.DeleteJobs(jobKeys, cancellationToken);
    }

    private async Task ScheduleJob(ITrigger trigger, CancellationToken cancellationToken)
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);

        try
        {
            await scheduler.UnscheduleJob(trigger.Key, cancellationToken);
            await scheduler.ScheduleJob(trigger, cancellationToken);
        }
        catch (SchedulerException e)
        {
            _logger.LogWarning(e, "Failed to schedule trigger {TriggerKey}", trigger.Key.ToString());
        }
    }

    private ITrigger CreateTrigger(IJob job, IElsaSchedule schedule, string[]? groupKeys)
    {
        var jobName = job.GetType().Name;
        var groupKey = BuildGroupKey(groupKeys);
        var triggerKey = new TriggerKey(job.JobId, groupKey);
        var json = _elsaJobSerializer.Serialize(job);
        var builder = TriggerBuilder.Create().ForJob(jobName).WithIdentity(triggerKey).UsingJobData(JobDataKey, json);

        switch (schedule)
        {
            case RecurringSchedule recurringSchedule:
            {
                builder.StartAt(recurringSchedule.StartAt);
                builder.WithSimpleSchedule(x => x.WithInterval(recurringSchedule.Interval).RepeatForever());
                break;
            }
            case CronSchedule cronSchedule:
            {
                builder.WithCronSchedule(cronSchedule.CronExpression);
                break;
            }
            case SpecificInstantSchedule specificInstantSchedule:
            {
                builder.StartAt(specificInstantSchedule.DateTime);
                break;
            }

            default:
                throw new NotSupportedException($"Schedule of type {schedule.GetType()} is not supported. But if you create an issue, we'll make this logic extensible & replaceable :)");
        }

        return builder.Build();
    }

    private static string BuildGroupKey(string[]? groupKeys)
    {
        var groupKeyInputs = new[] { RootGroupKey }.Concat(groupKeys ?? Array.Empty<string>());
        return string.Join(":", groupKeyInputs);
    }
}