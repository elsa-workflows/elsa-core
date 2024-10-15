using Elsa.Common.Multitenancy;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Http.HostedServices;

/// <summary>
/// Update the route table based on workflow triggers and bookmarks.
/// </summary>
[UsedImplicitly]
public class UpdateRouteTableHostedService(IServiceScopeFactory scopeFactory) : MultitenantBackgroundService(scopeFactory)
{
    protected override async Task ExecuteAsync(TenantScope tenantScope, CancellationToken stoppingToken)
    {
        var routeTableUpdater = tenantScope.ServiceProvider.GetRequiredService<IRouteTableUpdater>();
        await routeTableUpdater.UpdateAsync(stoppingToken);
    }
}