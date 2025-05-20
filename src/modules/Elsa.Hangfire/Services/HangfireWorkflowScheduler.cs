using Elsa.Common.Multitenancy;
using Elsa.Hangfire.Extensions;
using Elsa.Hangfire.Jobs;
using Elsa.Scheduling;
using Hangfire;
using Hangfire.Storage;

namespace Elsa.Hangfire.Services;

/// <summary>
/// An implementation of <see cref="IWorkflowScheduler"/> that uses Hangfire.
/// </summary>
public class HangfireWorkflowScheduler(
    IBackgroundJobClient backgroundJobClient, 
    IRecurringJobManager recurringJobManager,
    ITenantAccessor tenantAccessor,
    JobStorage jobStorage) : IWorkflowScheduler
{
    /// <inheritdoc />
    public ValueTask ScheduleAtAsync(string taskName, ScheduleNewWorkflowInstanceRequest request, DateTimeOffset at, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantAccessor.Tenant?.Id;
        backgroundJobClient.Schedule<RunWorkflowJob>(job => job.ExecuteAsync(taskName, request, tenantId, CancellationToken.None), at);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask ScheduleAtAsync(string taskName, ScheduleExistingWorkflowInstanceRequest request, DateTimeOffset at, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantAccessor.Tenant?.Id;
        backgroundJobClient.Schedule<ResumeWorkflowJob>(job => job.ExecuteAsync(taskName, request, tenantId, CancellationToken.None), at);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public async ValueTask ScheduleRecurringAsync(string taskName, ScheduleNewWorkflowInstanceRequest request, DateTimeOffset startAt, TimeSpan interval, CancellationToken cancellationToken = default)
    {
        await ScheduleCronAsync(taskName, request, interval.ToCronExpression(), cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask ScheduleRecurringAsync(string taskName, ScheduleExistingWorkflowInstanceRequest request, DateTimeOffset startAt, TimeSpan interval, CancellationToken cancellationToken = default)
    {
        await ScheduleCronAsync(taskName, request, interval.ToCronExpression(), cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask ScheduleCronAsync(string taskName, ScheduleNewWorkflowInstanceRequest request, string cronExpression, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantAccessor.Tenant?.Id;
        recurringJobManager.AddOrUpdate<RunWorkflowJob>(taskName, job => job.ExecuteAsync(taskName, request, tenantId, CancellationToken.None), cronExpression);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask ScheduleCronAsync(string taskName, ScheduleExistingWorkflowInstanceRequest request, string cronExpression, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantAccessor.Tenant?.Id;
        recurringJobManager.AddOrUpdate<ResumeWorkflowJob>(taskName, job => job.ExecuteAsync(taskName, request, tenantId, CancellationToken.None), cronExpression);
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
        foreach (var jobId in scheduledJobIds) backgroundJobClient.Delete(jobId);
        
        var queuedJobsIds = GetQueuedJobIds(taskName);
        foreach (var jobId in queuedJobsIds) backgroundJobClient.Delete(jobId);
        
        var recurringJobIds = GetRecurringJobIds<RunWorkflowJob>(taskName);
        foreach (var jobId in recurringJobIds) recurringJobManager.RemoveIfExists(jobId);
    }
    
    private IEnumerable<string> GetScheduledJobIds(string taskName)
    {
        return jobStorage.EnumerateScheduledJobs(taskName)
            .Select(x => x.Key)
            .Distinct()
            .ToList();
    }
    
    private IEnumerable<string> GetQueuedJobIds(string taskName)
    {
        return jobStorage.EnumerateQueuedJobs("default", taskName)
            .Select(x => x.Key)
            .Distinct()
            .ToList();
    }
    
    private IEnumerable<string> GetRecurringJobIds<TJob>(string taskName)
    {
        using var connection = jobStorage.GetConnection();
        var jobs = connection.GetRecurringJobs().Where(x => x.Job.Type == typeof(TJob) && (string)x.Job.Args[0] == taskName);
        return jobs.Select(x => x.Id).Distinct().ToList();
    }
}