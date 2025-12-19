using CShells.Features;
using Elsa.Common.Multitenancy;
using Elsa.Common.Multitenancy.EventHandlers;
using Elsa.Common.Multitenancy.HostedServices;
using Elsa.Common.RecurringTasks;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Common.ShellFeatures;

public class MultitenancyFeature : IShellFeature
{
    private Func<IServiceProvider, ITenantsProvider> _tenantsProviderFactory = sp => sp.GetRequiredService<DefaultTenantsProvider>();

    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSingleton<ITenantScopeFactory, DefaultTenantScopeFactory>()
            .AddSingleton<ITenantAccessor, DefaultTenantAccessor>()
            .AddSingleton<ITenantFinder, DefaultTenantFinder>()
            .AddSingleton<ITenantService, DefaultTenantService>()
            
            // Order is important: Startup task first, then background and recurring tasks.
            .AddSingleton<ITenantActivatedEvent, RunStartupTasks>()
            
            .AddSingleton<RunBackgroundTasks>()
            .AddSingleton<ITenantActivatedEvent>(sp => sp.GetRequiredService<RunBackgroundTasks>())
            .AddSingleton<ITenantDeactivatedEvent>(sp => sp.GetRequiredService<RunBackgroundTasks>())
            
            .AddSingleton<StartRecurringTasks>()
            .AddSingleton<ITenantActivatedEvent>(sp => sp.GetRequiredService<StartRecurringTasks>())
            .AddSingleton<ITenantDeactivatedEvent>(sp => sp.GetRequiredService<StartRecurringTasks>())
            
            .AddSingleton<RecurringTaskScheduleManager>()
            .AddSingleton<TenantEventsManager>()
            .AddScoped<DefaultTenantsProvider>()
            .AddScoped<DefaultTenantResolver>()
            .AddScoped<ITaskExecutor, TaskExecutor>()
            .AddScoped<IBackgroundTaskStarter, TaskExecutor>()
            .AddScoped(_tenantsProviderFactory)
            
            .AddHostedService<ActivateTenants>()
            ;
    }
}