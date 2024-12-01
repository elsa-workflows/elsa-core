using Elsa.Common.Multitenancy;
using Elsa.Common.Multitenancy.EventHandlers;
using Elsa.Common.Multitenancy.HostedServices;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Common.Features;

public class MultitenancyFeature(IModule module) : FeatureBase(module)
{
    private Func<IServiceProvider, ITenantsProvider> _tenantsProviderFactory = sp => sp.GetRequiredService<DefaultTenantsProvider>();
    
    public MultitenancyFeature UseTenantsProvider<TTenantsProvider>() where TTenantsProvider : class, ITenantsProvider
    {
        Services.TryAddScoped<TTenantsProvider>();
        return UseTenantsProvider(sp => sp.GetRequiredService<TTenantsProvider>());
    }
    
    public MultitenancyFeature UseTenantsProvider(Func<IServiceProvider, ITenantsProvider> tenantsProviderFactory)
    {
        _tenantsProviderFactory = tenantsProviderFactory;
        return this;
    }

    // public override void ConfigureHostedServices()
    // {
    //     Module
    //         .ConfigureHostedService<StartupTasksRunner>(1)
    //         .ConfigureHostedService<BackgroundTasksRunner>(1)
    //         .ConfigureHostedService<RecurringTasksRunner>(1)
    //         ;
    // }

    public override void Apply()
    {
        Services
            .AddSingleton<ITenantScopeFactory, DefaultTenantScopeFactory>()
            .AddSingleton<ITenantAccessor, DefaultTenantAccessor>()
            .AddSingleton<ITenantFinder, DefaultTenantFinder>()
            .AddSingleton<ITenantContextInitializer, DefaultTenantContextInitializer>()
            .AddSingleton<ITenantService, DefaultTenantService>()
            .AddSingleton<ITenantActivatedEvent, RunBackgroundTasks>()
            .AddSingleton<ITenantActivatedEvent, RunStartupTasks>()
            .AddSingleton<ITenantActivatedEvent, StartRecurringTasks>()
            .AddSingleton<ITenantDeactivatedEvent, RunBackgroundTasks>()
            .AddSingleton<ITenantDeactivatedEvent, StartRecurringTasks>()
            .AddScoped<DefaultTenantsProvider>()
            .AddScoped<DefaultTenantResolver>()
            .AddScoped(_tenantsProviderFactory);
    }
}