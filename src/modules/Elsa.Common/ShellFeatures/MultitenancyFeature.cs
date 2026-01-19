using CShells.Features;
using CShells.Hosting;
using Elsa.Common.Multitenancy;
using Elsa.Common.Multitenancy.EventHandlers;
using Elsa.Common.Multitenancy.HostedServices;
using Elsa.Common.RecurringTasks;
using Elsa.Common.ShellHandlers;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Common.ShellFeatures;

public class MultitenancyFeature : IShellFeature
{
    private readonly Func<IServiceProvider, ITenantsProvider> _tenantsProviderFactory = sp => sp.GetRequiredService<DefaultTenantsProvider>();

    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSingleton<ITenantScopeFactory, DefaultTenantScopeFactory>()
            .AddSingleton<ITenantAccessor, DefaultTenantAccessor>()
            .AddSingleton<ITenantFinder, DefaultTenantFinder>()
            .AddSingleton<ITenantService, DefaultTenantService>()
            
            // TenantTaskManager handles all task lifecycle in the correct order
            .AddSingleton<TenantTaskManager>()
            .AddSingleton<ITenantActivatedEvent>(sp => sp.GetRequiredService<TenantTaskManager>())
            .AddSingleton<ITenantDeactivatedEvent>(sp => sp.GetRequiredService<TenantTaskManager>())
            
            .AddSingleton<RecurringTaskScheduleManager>()
            .AddSingleton<TenantEventsManager>()
            .AddScoped<DefaultTenantsProvider>()
            .AddScoped<DefaultTenantResolver>()
            .AddScoped<ITaskExecutor, TaskExecutor>()
            .AddScoped<IBackgroundTaskStarter, TaskExecutor>()
            .AddScoped(_tenantsProviderFactory)
            
            .AddSingleton<IShellActivatedHandler, ActivateShellTenants>()
            .AddSingleton<IShellDeactivatingHandler, ActivateShellTenants>()
            ;
    }
}