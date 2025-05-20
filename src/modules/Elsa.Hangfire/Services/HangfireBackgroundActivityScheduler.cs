using Elsa.Common.Multitenancy;
using Elsa.Hangfire.Jobs;
using Elsa.Hangfire.States;
using Elsa.Workflows.Runtime;
using Hangfire;
using Hangfire.States;

namespace Elsa.Hangfire.Services;

/// <summary>
/// Invokes activities from a background worker within the context of its workflow instance using Hangfire.
/// </summary>
public class HangfireBackgroundActivityScheduler(IBackgroundJobClient backgroundJobClient, ITenantAccessor tenantAccessor) : IBackgroundActivityScheduler
{
    public Task<string> CreateAsync(ScheduledBackgroundActivity scheduledBackgroundActivity, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantAccessor.Tenant?.Id;
        var jobId = backgroundJobClient.Create<ExecuteBackgroundActivityJob>(x => x.ExecuteAsync(scheduledBackgroundActivity, tenantId, CancellationToken.None), new PendingState());
        return Task.FromResult(jobId);
    }

    public Task ScheduleAsync(string jobId, CancellationToken cancellationToken = default)
    {
        using var connection = JobStorage.Current.GetConnection();
        var jobData = connection.GetJobData(jobId);
        if (jobData != null) backgroundJobClient.ChangeState(jobId, new EnqueuedState());
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<string> ScheduleAsync(ScheduledBackgroundActivity scheduledBackgroundActivity, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantAccessor.Tenant?.Id;
        var jobId = backgroundJobClient.Enqueue<ExecuteBackgroundActivityJob>(x => x.ExecuteAsync(scheduledBackgroundActivity, tenantId, CancellationToken.None));
        return Task.FromResult(jobId);
    }

    public Task UnscheduledAsync(string jobId, CancellationToken cancellationToken = default)
    {
        backgroundJobClient.Delete(jobId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task CancelAsync(string jobId, CancellationToken cancellationToken = default)
    {
        backgroundJobClient.Delete(jobId);
        return Task.CompletedTask;
    }
}