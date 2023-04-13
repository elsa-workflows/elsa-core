using Elsa.Hangfire.Extensions;
using Elsa.Hangfire.Jobs;
using Elsa.Scheduling.Contracts;
using Elsa.Workflows.Runtime.Models.Requests;
using Hangfire;
using Hangfire.Storage;

namespace Elsa.Hangfire.Services;

/// <summary>
/// An implementation of <see cref="Scheduling.Contracts.IWorkflowScheduler"/> that uses Hangfire.
/// </summary>
public class HangfireWorkflowScheduler : IWorkflowScheduler
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IRecurringJobManager _recurringJobManager;
    private readonly JobStorage _jobStorage;

    /// <summary>
    /// Initializes a new instance of the <see cref="HangfireWorkflowScheduler"/> class.
    /// </summary>
    public HangfireWorkflowScheduler(IBackgroundJobClient backgroundJobClient, IRecurringJobManager recurringJobManager, JobStorage jobStorage)
    {
        _backgroundJobClient = backgroundJobClient;
        _recurringJobManager = recurringJobManager;
        _jobStorage = jobStorage;
    }
    
    /// <inheritdoc />
    public ValueTask ScheduleAtAsync(string taskName, DispatchWorkflowDefinitionRequest request, DateTimeOffset at, CancellationToken cancellationToken = default)
    {
        _backgroundJobClient.Schedule<RunWorkflowJob>(job => job.ExecuteAsync(taskName, request, CancellationToken.None), at);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask ScheduleAtAsync(string taskName, DispatchWorkflowInstanceRequest request, DateTimeOffset at, CancellationToken cancellationToken = default)
    {
        _backgroundJobClient.Schedule<ResumeWorkflowJob>(job => job.ExecuteAsync(taskName, request, CancellationToken.None), at);
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
        _recurringJobManager.AddOrUpdate<RunWorkflowJob>(taskName, job => job.ExecuteAsync(taskName, request, CancellationToken.None), cronExpression);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask ScheduleCronAsync(string taskName, DispatchWorkflowInstanceRequest request, string cronExpression, CancellationToken cancellationToken = default)
    {
        _recurringJobManager.AddOrUpdate<ResumeWorkflowJob>(taskName, job => job.ExecuteAsync(taskName, request, CancellationToken.None), cronExpression);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask UnscheduleAsync(string taskName, CancellationToken cancellationToken = default)
    {
        DeleteJobByTaskName(taskName);
        return ValueTask.CompletedTask;
    }

    private void DeleteJobByTaskName(string taskName)
    {
        var scheduledJobIds = GetScheduledJobIds(taskName);
        foreach (var jobId in scheduledJobIds) _backgroundJobClient.Delete(jobId);
        
        var queuedJobsIds = GetQueuedJobIds(taskName);
        foreach (var jobId in queuedJobsIds) _backgroundJobClient.Delete(jobId);
        
        var recurringJobIds = GetRecurringJobIds<RunWorkflowJob>(taskName);
        foreach (var jobId in recurringJobIds) _recurringJobManager.RemoveIfExists(jobId);
    }
    
    private IEnumerable<string> GetScheduledJobIds(string taskName)
    {
        return _jobStorage.EnumerateScheduledJobs(taskName)
            .Select(x => x.Key)
            .Distinct()
            .ToList();
    }
    
    private IEnumerable<string> GetQueuedJobIds(string taskName)
    {
        return _jobStorage.EnumerateQueuedJobs("default", taskName)
            .Select(x => x.Key)
            .Distinct()
            .ToList();
    }
    
    private IEnumerable<string> GetRecurringJobIds<TJob>(string taskName)
    {
        using var connection = _jobStorage.GetConnection();
        var jobs = connection.GetRecurringJobs().Where(x => x.Job.Type == typeof(TJob) && (string)x.Job.Args[0] == taskName);
        return jobs.Select(x => x.Id).Distinct().ToList();
    }
}