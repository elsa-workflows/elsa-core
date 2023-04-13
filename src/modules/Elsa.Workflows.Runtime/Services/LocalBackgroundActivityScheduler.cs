using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Models;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// Invokes activities from a background worker within the context of its workflow instance using a local background worker.
/// </summary>
public class LocalBackgroundActivityScheduler : IBackgroundActivityScheduler
{
    private readonly IJobQueue _jobQueue;
    private readonly IBackgroundActivityInvoker _backgroundActivityInvoker;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalBackgroundActivityScheduler"/> class.
    /// </summary>
    public LocalBackgroundActivityScheduler(IJobQueue jobQueue, IBackgroundActivityInvoker backgroundActivityInvoker)
    {
        _jobQueue = jobQueue;
        _backgroundActivityInvoker = backgroundActivityInvoker;
    }

    /// <inheritdoc />
    public Task<string> ScheduleAsync(ScheduledBackgroundActivity scheduledBackgroundActivity, CancellationToken cancellationToken = default)
    {
        var jobId = _jobQueue.Enqueue(async ct => await InvokeBackgroundActivity(scheduledBackgroundActivity, ct));
        return Task.FromResult(jobId);
    }

    /// <inheritdoc />
    public Task CancelAsync(string jobId, CancellationToken cancellationToken = default)
    {
        _jobQueue.Cancel(jobId);
        return Task.CompletedTask;
    }
    
    private async Task InvokeBackgroundActivity(ScheduledBackgroundActivity scheduledBackgroundActivity, CancellationToken cancellationToken)
    {
        await _backgroundActivityInvoker.ExecuteAsync(scheduledBackgroundActivity, cancellationToken);
    }
}