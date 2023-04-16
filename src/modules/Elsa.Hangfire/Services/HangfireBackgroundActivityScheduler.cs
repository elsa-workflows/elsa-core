using Elsa.Hangfire.Jobs;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Models;
using Hangfire;

namespace Elsa.Hangfire.Services;

/// <summary>
/// Invokes activities from a background worker within the context of its workflow instance using Hangfire.
/// </summary>
public class HangfireBackgroundActivityScheduler : IBackgroundActivityScheduler
{
    private readonly IBackgroundJobClient _backgroundJobClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="HangfireBackgroundActivityScheduler"/> class.
    /// </summary>
    public HangfireBackgroundActivityScheduler(IBackgroundJobClient backgroundJobClient)
    {
        _backgroundJobClient = backgroundJobClient;
    }

    /// <inheritdoc />
    public Task<string> ScheduleAsync(ScheduledBackgroundActivity scheduledBackgroundActivity, CancellationToken cancellationToken = default)
    {
        var jobId = _backgroundJobClient.Enqueue<ExecuteBackgroundActivityJob>(x => x.ExecuteAsync(scheduledBackgroundActivity, CancellationToken.None));
        return Task.FromResult(jobId);
    }

    /// <inheritdoc />
    public Task CancelAsync(string jobId, CancellationToken cancellationToken = default)
    {
        _backgroundJobClient.Delete(jobId);
        return Task.CompletedTask;
    }
}