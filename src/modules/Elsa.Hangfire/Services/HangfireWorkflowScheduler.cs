using Elsa.Hangfire.Extensions;
using Elsa.Hangfire.Jobs;
using Elsa.Scheduling.Contracts;
using Elsa.Workflows.Runtime.Models.Requests;
using Hangfire;

namespace Elsa.Hangfire.Services;

/// <summary>
/// An implementation of <see cref="Scheduling.Contracts.IWorkflowScheduler"/> that uses Hangfire.
/// </summary>
public class HangfireWorkflowScheduler : IWorkflowScheduler
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IRecurringJobManager _recurringJobManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="HangfireWorkflowScheduler"/> class.
    /// </summary>
    public HangfireWorkflowScheduler(IBackgroundJobClient backgroundJobClient, IRecurringJobManager recurringJobManager)
    {
        _backgroundJobClient = backgroundJobClient;
        _recurringJobManager = recurringJobManager;
    }
    
    /// <inheritdoc />
    public ValueTask ScheduleAtAsync(string taskName, DispatchWorkflowDefinitionRequest request, DateTimeOffset at, CancellationToken cancellationToken = default)
    {
        _backgroundJobClient.Schedule<RunWorkflowJob>(job => job.ExecuteAsync(request), at);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask ScheduleAtAsync(string taskName, DispatchWorkflowInstanceRequest request, DateTimeOffset at, CancellationToken cancellationToken = default)
    {
        _backgroundJobClient.Schedule<ResumeWorkflowJob>(job => job.ExecuteAsync(request), at);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public async ValueTask ScheduleRecurringAsync(string taskName, DispatchWorkflowDefinitionRequest request, DateTimeOffset startAt, TimeSpan interval, CancellationToken cancellationToken = default)
    {
        await ScheduleCronAsync(taskName, request, interval.ToCronExpression(), cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask ScheduleRecurringAsync(string taskName, DispatchWorkflowInstanceRequest request, DateTimeOffset startAt, TimeSpan interval, CancellationToken cancellationToken = default)
    {
        await ScheduleCronAsync(taskName, request, interval.ToCronExpression(), cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask ScheduleCronAsync(string taskName, DispatchWorkflowDefinitionRequest request, string cronExpression, CancellationToken cancellationToken = default)
    {
        _recurringJobManager.AddOrUpdate<RunWorkflowJob>(taskName, job => job.ExecuteAsync(request), cronExpression);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask ScheduleCronAsync(string taskName, DispatchWorkflowInstanceRequest request, string cronExpression, CancellationToken cancellationToken = default)
    {
        _recurringJobManager.AddOrUpdate<ResumeWorkflowJob>(taskName, job => job.ExecuteAsync(request), cronExpression);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask UnscheduleAsync(string taskName, CancellationToken cancellationToken = default)
    {
        _backgroundJobClient.Delete(taskName);
        _recurringJobManager.RemoveIfExists(taskName);
        return ValueTask.CompletedTask;
    }
}