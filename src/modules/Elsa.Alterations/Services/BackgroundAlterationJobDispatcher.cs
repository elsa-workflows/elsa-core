using Elsa.Alterations.Core.Contracts;
using Elsa.Mediator.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Alterations.Services;

/// <summary>
/// Dispatches an alteration job for execution using an in-memory channel.
/// </summary>
public class BackgroundAlterationJobDispatcher(IJobQueue jobQueue, IServiceScopeFactory scopeFactory) : IAlterationJobDispatcher
{
    /// <inheritdoc />
    public ValueTask DispatchAsync(string jobId, CancellationToken cancellationToken = default)
    {
        jobQueue.Enqueue(ct => ExecuteJobAsync(jobId, ct));
        return default;
    }
    
    private async Task ExecuteJobAsync(string alterationJobId, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var alterationJobRunner = scope.ServiceProvider.GetRequiredService<IAlterationJobRunner>();
        await alterationJobRunner.RunAsync(alterationJobId, cancellationToken);
    }
}