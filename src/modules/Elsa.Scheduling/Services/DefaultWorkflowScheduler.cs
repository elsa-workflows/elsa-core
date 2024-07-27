using Elsa.Scheduling.Schedules;
using Elsa.Scheduling.Tasks;

namespace Elsa.Scheduling.Services;

/// <summary>
/// A default implementation of <see cref="IWorkflowScheduler"/> that uses the <see cref="LocalScheduler"/>.
/// </summary>
public class DefaultWorkflowScheduler(IScheduler scheduler) : IWorkflowScheduler
{
    /// <inheritdoc />
    public async ValueTask ScheduleAtAsync(string taskName, ScheduleNewWorkflowInstanceRequest request, DateTimeOffset at, CancellationToken cancellationToken = default)
    {
        await scheduler.ScheduleAsync(taskName, new RunWorkflowTask(request), new SpecificInstantSchedule(at), cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask ScheduleAtAsync(string taskName, ScheduleExistingWorkflowInstanceRequest request, DateTimeOffset at, CancellationToken cancellationToken = default)
    {
        var task = new ResumeWorkflowTask(request);
        var schedule = new SpecificInstantSchedule(at);
        await scheduler.ScheduleAsync(taskName, task, schedule, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask ScheduleRecurringAsync(string taskName, ScheduleNewWorkflowInstanceRequest request, DateTimeOffset startAt, TimeSpan interval, CancellationToken cancellationToken = default)
    {
        var task = new RunWorkflowTask(request);
        var schedule = new RecurringSchedule(startAt, interval);
        await scheduler.ScheduleAsync(taskName, task, schedule, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask ScheduleRecurringAsync(string taskName, ScheduleExistingWorkflowInstanceRequest request, DateTimeOffset startAt, TimeSpan interval, CancellationToken cancellationToken = default)
    {
        var task = new ResumeWorkflowTask(request);
        var schedule = new RecurringSchedule(startAt, interval);
        await scheduler.ScheduleAsync(taskName, task, schedule, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask ScheduleCronAsync(string taskName, ScheduleNewWorkflowInstanceRequest request, string cronExpression, CancellationToken cancellationToken = default)
    {
        var task = new RunWorkflowTask(request);
        var schedule = new CronSchedule(cronExpression);
        await scheduler.ScheduleAsync(taskName, task, schedule, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask ScheduleCronAsync(string taskName, ScheduleExistingWorkflowInstanceRequest request, string cronExpression, CancellationToken cancellationToken = default)
    {
        var task = new ResumeWorkflowTask(request);
        var schedule = new CronSchedule(cronExpression);
        await scheduler.ScheduleAsync(taskName, task, schedule, cancellationToken);
    }
    
    /// <inheritdoc />
    public async ValueTask UnscheduleAsync(string taskName, CancellationToken cancellationToken = default)
    {
        await scheduler.ClearScheduleAsync(taskName, cancellationToken);
    }
}