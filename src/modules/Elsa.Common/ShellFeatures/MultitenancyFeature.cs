using CShells.Features;
using CShells.Lifecycle;
using Elsa.Common.Multitenancy;
using Elsa.Common.Multitenancy.EventHandlers;
using Elsa.Common.Multitenancy.HostedServices;
using Elsa.Common.RecurringTasks;
using Elsa.Common.ShellHandlers;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Common.ShellFeatures;

[ShellFeature("Multitenancy")]
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
            
            // Coordinate tenant task lifecycle separately from the tenant event pipeline.
            .AddSingleton<TenantTaskLifecycleCoordinator>()
            .AddSingleton<TenantTaskLifecycleEventHandler>()
            .AddSingleton<ITenantActivatedEvent>(sp => sp.GetRequiredService<TenantTaskLifecycleEventHandler>())
            .AddSingleton<ITenantDeactivatedEvent>(sp => sp.GetRequiredService<TenantTaskLifecycleEventHandler>())
            
            .AddSingleton<RecurringTaskScheduleManager>()
            .AddSingleton<TenantEventsManager>()
            .AddScoped<DefaultTenantsProvider>()
            .AddScoped<DefaultTenantResolver>()
            .AddScoped<ITaskExecutor, TaskExecutor>()
            .AddScoped<IBackgroundTaskStarter, TaskExecutor>()
            .AddScoped(_tenantsProviderFactory)
            
            // Transient per CShells 0.0.15 convention — the registry resolves IEnumerable<IShellInitializer> and
            // IEnumerable<IDrainHandler> on demand from the shell's IServiceProvider during activation/draining.
            .AddTransient<IShellInitializer, ActivateShellTenants>()
            .AddTransient<IDrainHandler, ActivateShellTenants>()
            ;
    }
}