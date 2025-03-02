using Elsa.Common.Features;
using Elsa.Common.Multitenancy;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Framework.Shells;
using Elsa.Framework.Tenants;
using Elsa.Tenants.Options;
using Elsa.Tenants.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Tenants.Features;

/// <summary>
/// Configures multi-tenancy features.
/// </summary>
[DependencyOf(typeof(MultitenancyFeature))]
public class TenantsFeature(IModule serviceConfiguration) : FeatureBase(serviceConfiguration)
{
    /// <summary>
    /// Configures the Tenants options.
    /// </summary>
    private Action<MultitenancyOptions> MultitenancyOptions { get; set; } = _ => { };
    
    private Action<TenantsOptions> TenantsOptions { get; set; } = _ => { };
    
    public TenantsFeature ConfigureMultitenancy(Action<MultitenancyOptions> configure)
    {
        Services.Configure(configure);
        return this;
    }
    
    public TenantsFeature ConfigureTenants(Action<TenantsOptions> configure)
    {
        Services.Configure(configure);
        return this;
    }
    
    public void UseConfigurationBasedTenantsProvider(Action<TenantsOptions> configure)
    {
        ConfigureTenants(configure);
        Module.Configure<MultitenancyFeature>(feature => feature.UseTenantsProvider<ConfigurationTenantsProvider>());
    }
    
    public void UseStoreBasedTenantsProvider()
    {
        Module.Configure<MultitenancyFeature>(feature => feature.UseTenantsProvider<StoreTenantsProvider>());
    }

    public override void ConfigureHostedServices()
    {
        //ConfigureHostedService<CreateShellsHostedService>();
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.Configure(MultitenancyOptions);

        Services
            .AddScoped<ITenantResolverPipelineInvoker, DefaultTenantResolverPipelineInvoker>()
            .AddScoped<ITenantResolver, DefaultTenantResolver>();
    }
}