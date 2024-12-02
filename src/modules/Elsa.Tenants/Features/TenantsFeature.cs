using Elsa.Common.Features;
using Elsa.Common.Multitenancy;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Tenants.Options;
using Elsa.Tenants.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Tenants.Features;

/// <summary>
/// Configures multi-tenancy features.
/// </summary>
public class TenantsFeature(IModule serviceConfiguration) : FeatureBase(serviceConfiguration)
{
    /// <summary>
    /// Configures the Tenants options.
    /// </summary>
    private Action<MultitenancyOptions> TenantsOptions { get; set; } = _ => { };
    
    public TenantsFeature ConfigureOptions(Action<MultitenancyOptions> configure)
    {
        Services.Configure(configure);
        return this;
    }
    
    public void UseConfigurationBasedTenantsProvider()
    {
        Module.Configure<MultitenancyFeature>(feature => feature.UseTenantsProvider<ConfigurationTenantsProvider>());
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.Configure(TenantsOptions);

        Services
            .AddScoped<ITenantResolverPipelineInvoker, DefaultTenantResolverPipelineInvoker>()
            .AddScoped<ITenantResolver, DefaultTenantResolver>();
    }
}