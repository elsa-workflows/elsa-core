using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Common.Multitenancy.HostedServices;

public class StartupTasksRunner(IServiceScopeFactory serviceScopeFactory) : MultitenantHostedService(serviceScopeFactory)
{
    protected override async Task StartAsync(TenantScope tenantScope, CancellationToken cancellationToken)
    {
        var tasks = tenantScope.ServiceProvider.GetServices<IStartupTask>();
        foreach (var task in tasks) await task.ExecuteAsync(cancellationToken);
    }
}