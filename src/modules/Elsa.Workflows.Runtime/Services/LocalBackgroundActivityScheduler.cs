using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// Invokes activities from a background worker within the context of its workflow instance using a local background worker.
/// </summary>
public class LocalBackgroundActivityScheduler(IJobQueue jobQueue, IServiceScopeFactory scopeFactory) : IBackgroundActivityScheduler
{
    /// <inheritdoc />
    public Task<string> ScheduleAsync(ScheduledBackgroundActivity scheduledBackgroundActivity, CancellationToken cancellationToken = default)
    {
        var jobId = jobQueue.Enqueue(async ct => await InvokeBackgroundActivity(scheduledBackgroundActivity, ct));
        return Task.FromResult(jobId);
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