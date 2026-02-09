using CShells.Features;
using Elsa.Common.Multitenancy;
using Elsa.Tenants.AspNetCore.Options;
using Elsa.Tenants.AspNetCore.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Tenants.AspNetCore.ShellFeatures;

/// <summary>
/// Provides multi-tenant HTTP routing capabilities.
/// </summary>
[ShellFeature(
    DisplayName = "Multi-tenant HTTP Routing",
    Description = "Enables tenant resolution and routing for HTTP requests",
    DependencyOf = ["Http", "Tenants"])]
public class MultitenantHttpRoutingFeature : IShellFeature
{
    /// <summary>
    /// The name of the HTTP header used to identify the tenant. Defaults to "X-Tenant-Id".
    /// </summary>
    public string TenantHeaderName { get; set; } = "X-Tenant-Id";

    public void ConfigureServices(IServiceCollection services)
    {
        // Multitenancy HTTP options.
        services.Configure<MultitenancyHttpOptions>(options =>
        {
            options.TenantHeaderName = TenantHeaderName;
        });
        
        // Tenant resolvers.
        services
            .AddScoped<ITenantResolver, RoutePrefixTenantResolver>()
            .AddScoped<ITenantResolver, HeaderTenantResolver>()
            .AddScoped<ITenantResolver, HostTenantResolver>()
            .AddScoped<TenantPrefixHttpEndpointRoutesProvider>();
    }
}
