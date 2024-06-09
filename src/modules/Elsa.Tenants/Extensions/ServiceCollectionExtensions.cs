using Elsa.Framework.Shells;
using Elsa.Framework.Tenants;
using Elsa.Tenants;
using Elsa.Tenants.Configuration;
using Elsa.Tenants.Options;
using Elsa.Tenants.Providers;
using Elsa.Tenants.Resolvers;
using Elsa.Tenants.Services;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTenants(this IServiceCollection services, Action<TenantsConfiguration>? configure = null)
    {
        var configuration = new TenantsConfiguration();
        configure?.Invoke(configuration);
        services.Configure(configuration.MultitenancyOptions);
        
        return services
            .ConfigureOptions<MultitenancyOptions>()
            .AddTransient<PipelinedTenantResolver>()
            .AddSingleton<ConfigurationTenantsProvider>()
            .AddSingleton<IAmbientTenantAccessor, AmbientTenantAccessor>()
            .AddSingleton<IApplicationServicesAccessor>(new DefaultApplicationServicesAccessor(services))
            .AddScoped<ConfigurationTenantsProvider>()
            .AddScoped<ITenantResolutionStrategy, AmbientTenantResolver>();
    }
}