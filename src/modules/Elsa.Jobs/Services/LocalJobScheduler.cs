using Elsa.Common.Contracts;
using Elsa.Jobs.Contracts;
using Elsa.Jobs.ScheduledJobs;
using Elsa.Jobs.Schedules;

namespace Elsa.Jobs.Services;

public class LocalJobScheduler : IJobScheduler
{
    private readonly ISystemClock _systemClock;
    private readonly IJobRunner _jobRunner;
    private readonly IDictionary<string, IScheduledJob> _scheduledJobs = new Dictionary<string, IScheduledJob>();

    public LocalJobScheduler(ISystemClock systemClock, IJobRunner jobRunner)
    {
        _systemClock = systemClock;
        _jobRunner = jobRunner;
    }

    public Task ScheduleAsync(IJob job, string name, ISchedule schedule, string[]? groupKeys = default, CancellationToken cancellationToken = default)
    {
        switch (schedule)
        {
            case CronSchedule cronSchedule:
            {
                var scheduledJob = new ScheduledCronJob(name, job, _systemClock, cronSchedule.CronExpression, _jobRunner, cancellationToken);
                RegisterScheduledJob(scheduledJob);
                break;
            }
            case RecurringSchedule recurringSchedule:
            {
                var scheduledJob = new ScheduledRecurringJob(name, job, _systemClock, recurringSchedule.StartAt, recurringSchedule.Interval, _jobRunner, cancellationToken);
                RegisterScheduledJob(scheduledJob);
                break;
            }
            case SpecificInstantSchedule specificInstantSchedule:
            {
                var scheduledJob = new ScheduledSpecificInstantJob(name, job, _systemClock, specificInstantSchedule.DateTime, _jobRunner, cancellationToken);
                RegisterScheduledJob(scheduledJob);
                break;
            }
            default:
                throw new NotSupportedException($"Schedule of type {schedule.GetType()} is not supported.");
        }

        return Task.CompletedTask;
    }

    private void RegisterScheduledJob(IScheduledJob scheduledJob)
    {
        if (_scheduledJobs.TryGetValue(scheduledJob.Name, out var existingScheduledJob)) existingScheduledJob.Cancel();
        _scheduledJobs[scheduledJob.Name] = scheduledJob;
    }

    public Task UnscheduleAsync(string name, CancellationToken cancellationToken = default)
    {
        if (_scheduledJobs.TryGetValue(name, out var existingScheduledJob)) existingScheduledJob.Cancel();
        return Task.CompletedTask;
    }

    public Task ClearAsync(string[]? groupKeys = default, CancellationToken cancellationToken = default)
    {
        foreach (var scheduledJob in _scheduledJobs.Values) scheduledJob.Cancel();

        _scheduledJobs.Clear();
        return Task.CompletedTask;
    }
}