using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Multitenancy;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.HostedServices
{
    public class StartupRunnerHostedService : IHostedService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ITenantStore _tenantStore;

        public StartupRunnerHostedService(IServiceScopeFactory scopeFactory, ITenantStore tenantStore)
        {
            _scopeFactory = scopeFactory;
            _tenantStore = tenantStore;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            foreach (var tenant in await _tenantStore.GetTenantsAsync())
            {
                using var scope = _scopeFactory.CreateScopeForTenant(tenant);

                var startupRunner = scope.ServiceProvider.GetRequiredService<IStartupRunner>();
                await startupRunner.StartupAsync(cancellationToken);
            }

            using var sharedScope = _scopeFactory.CreateScope();
            var sharedStartupRunner = sharedScope.ServiceProvider.GetRequiredService<ISharedStartupRunner>();
            await sharedStartupRunner.StartupAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}