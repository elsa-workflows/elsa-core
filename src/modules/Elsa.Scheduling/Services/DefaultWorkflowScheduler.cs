using Elsa.Scheduling.Contracts;
using Elsa.Scheduling.Schedules;
using Elsa.Scheduling.Tasks;
using Elsa.Workflows.Runtime.Requests;

namespace Elsa.Scheduling.Services;

/// <summary>
/// A default implementation of <see cref="IWorkflowScheduler"/> that uses the <see cref="LocalScheduler"/>.
/// </summary>
public class DefaultWorkflowScheduler(IScheduler scheduler) : IWorkflowScheduler
{
    /// <inheritdoc />
    public async ValueTask ScheduleAtAsync(string taskName, DispatchWorkflowDefinitionRequest request, DateTimeOffset at, CancellationToken cancellationToken = default)
    {
        await scheduler.ScheduleAsync(taskName, new RunWorkflowTask(request), new SpecificInstantSchedule(at), cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask ScheduleAtAsync(string taskName, DispatchWorkflowInstanceRequest request, DateTimeOffset at, CancellationToken cancellationToken = default)
    {
        var task = new ResumeWorkflowTask(request);
        var schedule = new SpecificInstantSchedule(at);
        await scheduler.ScheduleAsync(taskName, task, schedule, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask ScheduleRecurringAsync(string taskName, DispatchWorkflowDefinitionRequest request, DateTimeOffset startAt, TimeSpan interval, CancellationToken cancellationToken = default)
    {
        var task = new RunWorkflowTask(request);
        var schedule = new RecurringSchedule(startAt, interval);
        await scheduler.ScheduleAsync(taskName, task, schedule, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask ScheduleRecurringAsync(string taskName, DispatchWorkflowInstanceRequest request, DateTimeOffset startAt, TimeSpan interval, CancellationToken cancellationToken = default)
    {
        var task = new ResumeWorkflowTask(request);
        var schedule = new RecurringSchedule(startAt, interval);
        await scheduler.ScheduleAsync(taskName, task, schedule, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask ScheduleCronAsync(string taskName, DispatchWorkflowDefinitionRequest request, string cronExpression, CancellationToken cancellationToken = default)
    {
        var task = new RunWorkflowTask(request);
        var schedule = new CronSchedule(cronExpression);
        await scheduler.ScheduleAsync(taskName, task, schedule, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask ScheduleCronAsync(string taskName, DispatchWorkflowInstanceRequest request, string cronExpression, CancellationToken cancellationToken = default)
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