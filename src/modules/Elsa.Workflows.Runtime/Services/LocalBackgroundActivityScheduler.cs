using Elsa.Mediator.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Invokes activities from a background worker within the context of its workflow instance using a local background worker.
/// </summary>
public class LocalBackgroundActivityScheduler(IJobQueue jobQueue, IServiceScopeFactory scopeFactory) : IBackgroundActivityScheduler
{
    public Task<string> CreateAsync(ScheduledBackgroundActivity scheduledBackgroundActivity, CancellationToken cancellationToken = default)
    {
        var jobId = jobQueue.Create(async ct => await InvokeBackgroundActivity(scheduledBackgroundActivity, ct));
        return Task.FromResult(jobId);
    }

    public Task ScheduleAsync(string jobId, CancellationToken cancellationToken = default)
    {
        jobQueue.Enqueue(jobId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<string> ScheduleAsync(ScheduledBackgroundActivity scheduledBackgroundActivity, CancellationToken cancellationToken = default)
    {
        var jobId = jobQueue.Enqueue(async ct => await InvokeBackgroundActivity(scheduledBackgroundActivity, ct));
        return Task.FromResult(jobId);
    }

    public Task UnscheduledAsync(string jobId, CancellationToken cancellationToken = default)
    {
        jobQueue.Dequeue(jobId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task CancelAsync(string jobId, CancellationToken cancellationToken = default)
    {
        jobQueue.Cancel(jobId);
        return Task.CompletedTask;
    }
    
    private async Task InvokeBackgroundActivity(ScheduledBackgroundActivity scheduledBackgroundActivity, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var backgroundActivityInvoker = scope.ServiceProvider.GetRequiredService<IBackgroundActivityInvoker>();
        await backgroundActivityInvoker.ExecuteAsync(scheduledBackgroundActivity, cancellationToken);
    }
}