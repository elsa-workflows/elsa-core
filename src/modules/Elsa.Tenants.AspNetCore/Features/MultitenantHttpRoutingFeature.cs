using Elsa.Common.Multitenancy;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Http.Features;
using Elsa.Tenants.AspNetCore.Options;
using Elsa.Tenants.AspNetCore.Services;
using Elsa.Tenants.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Tenants.AspNetCore.Features;

[DependencyOf(typeof(HttpFeature))]
[DependencyOf(typeof(TenantsFeature))]
public class MultitenantHttpRoutingFeature(IModule module) : FeatureBase(module)
{
    private Action<MultitenancyHttpOptions> _configureMultitenancyHttpOptions = _ => { };

    /// <summary>
    /// Configures the MultitenantHttpRoutingFeature to use a specific tenant header name.
    /// </summary>
    /// <param name="headerName">The name of the HTTP header used to identify the tenant.</param>
    /// <returns>The current instance of <see cref="MultitenantHttpRoutingFeature"/> for fluent configuration.</returns>
    public MultitenantHttpRoutingFeature WithTenantHeader(string headerName) => WithMultitenancyHttpOptions(options => options.TenantHeaderName = headerName);

    /// <summary>
    /// Configures the MultitenantHttpRoutingFeature with custom multitenancy HTTP options.
    /// </summary>
    /// <param name="configure">The action to configure <see cref="MultitenancyHttpOptions"/>.</param>
    /// <returns>The current instance of <see cref="MultitenantHttpRoutingFeature"/> for fluent configuration.</returns>
    public MultitenantHttpRoutingFeature WithMultitenancyHttpOptions(Action<MultitenancyHttpOptions> configure)
    {
        _configureMultitenancyHttpOptions = configure;
        return this;
    }
    
    public override void Configure()
    {
        Module.Configure<HttpFeature>(feature =>
        {
            feature.WithHttpEndpointRoutesProvider<TenantPrefixHttpEndpointRoutesProvider>();
            feature.WithHttpEndpointBasePathProvider<TenantPrefixHttpEndpointBasePathProvider>();
        });
    }

    public override void Apply()
    {
        // Multitenancy HTTP options.
        Services.Configure(_configureMultitenancyHttpOptions);
        
        // Tenant resolvers.
        Services
            .AddScoped<ITenantResolver, RoutePrefixTenantResolver>()
            .AddScoped<ITenantResolver, HeaderTenantResolver>()
            .AddScoped<ITenantResolver, HostTenantResolver>()
            .AddScoped<TenantPrefixHttpEndpointRoutesProvider>();
    }
}