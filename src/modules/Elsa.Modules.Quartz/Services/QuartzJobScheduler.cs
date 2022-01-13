using Elsa.Activities.Scheduling.Schedules;
using Elsa.Modules.Quartz.Contracts;
using Elsa.Modules.Quartz.Jobs;
using Microsoft.Extensions.Logging;
using Quartz;
using IElsaJobScheduler = Elsa.Activities.Scheduling.Contracts.IJobScheduler;
using IElsaJob = Elsa.Activities.Scheduling.Contracts.IJob;
using IElsaSchedule = Elsa.Activities.Scheduling.Contracts.ISchedule;

namespace Elsa.Modules.Quartz.Services;

public class QuartzJobScheduler : IElsaJobScheduler
{
    public const string JobDataKey = "ElsaJob";
    private readonly IElsaJobSerializer _elsaJobSerializer;
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly ILogger _logger;

    public QuartzJobScheduler(IElsaJobSerializer elsaJobSerializer, ISchedulerFactory schedulerFactory, ILogger<QuartzJobScheduler> logger)
    {
        _elsaJobSerializer = elsaJobSerializer;
        _schedulerFactory = schedulerFactory;
        _logger = logger;
    }

    public async Task ScheduleAsync(IElsaJob job, IElsaSchedule schedule, CancellationToken cancellationToken = default)
    {
        var quartzTrigger = CreateTrigger(job, schedule);
        await ScheduleJob(quartzTrigger, cancellationToken);
    }
    
    private async Task ScheduleJob(ITrigger trigger, CancellationToken cancellationToken)
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
        
        try
        {
            await scheduler.ScheduleJob(trigger, cancellationToken);
        }
        catch (SchedulerException e)
        {
            _logger.LogWarning(e, "Failed to schedule trigger {TriggerKey}", trigger.Key.ToString());
        }
    }

    private ITrigger CreateTrigger(IElsaJob job, IElsaSchedule schedule)
    {
        var jobName = job.GetType().Name;
        var json = _elsaJobSerializer.Serialize(job);
        var builder = TriggerBuilder.Create().ForJob(jobName).WithIdentity(job.JobId).UsingJobData(JobDataKey, json);

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
}