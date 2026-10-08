using Elsa.Alterations.Core.Contracts;
using Elsa.Common.Multitenancy;
using Elsa.Mediator.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Alterations.Services;

/// <summary>
/// Dispatches an alteration job for execution using an in-memory channel.
/// </summary>
public class BackgroundAlterationJobDispatcher(
    IJobQueue jobQueue,
    ITenantAccessor tenantAccessor,
    ITenantScopeFactory tenantScopeFactory) : IAlterationJobDispatcher
{
    /// <inheritdoc />
    public ValueTask DispatchAsync(string jobId, CancellationToken cancellationToken = default)
    {
        var tenant = tenantAccessor.Tenant;
        jobQueue.Enqueue(ct => ExecuteJobAsync(jobId, tenant, ct));
        return default;
    }
    
    private async Task ExecuteJobAsync(string alterationJobId, Tenant? tenant, CancellationToken cancellationToken)
    {
        await using var tenantScope = tenantScopeFactory.CreateScope(tenant);
        var alterationJobRunner = tenantScope.ServiceProvider.GetRequiredService<IAlterationJobRunner>();
        await alterationJobRunner.RunAsync(alterationJobId, cancellationToken);
    }
}
