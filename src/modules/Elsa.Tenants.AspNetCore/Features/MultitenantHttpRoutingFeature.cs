using Elsa.Common.Multitenancy;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Http.Features;
using Elsa.Tenants.AspNetCore.Services;
using Elsa.Tenants.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Tenants.AspNetCore.Features;

[DependencyOf(typeof(HttpFeature))]
[DependencyOf(typeof(TenantsFeature))]
public class MultitenantHttpRoutingFeature(IModule module) : FeatureBase(module)
{
    public override void Configure()
    {
        Module.Configure<HttpFeature>(feature =>
        {
            feature.WithHttpEndpointRoutesProvider<TenantPrefixHttpEndpointRoutesProvider>();
        });
    }

    public override void Apply()
    {
        // Tenant resolvers.
        Services
            .AddScoped<ITenantResolver, RoutePrefixTenantResolver>()
            .AddScoped<ITenantResolver, HeaderTenantResolver>()
            .AddScoped<ITenantResolver, HostTenantResolver>()
            .AddScoped<TenantPrefixHttpEndpointRoutesProvider>();
    }
}