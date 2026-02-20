using CShells.Features;
using Elsa.Common.Multitenancy;
using Elsa.Tenants.AspNetCore.Options;
using Elsa.Tenants.AspNetCore.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Tenants.AspNetCore.ShellFeatures;

/// <summary>
/// Provides multi-tenant HTTP routing features.
/// </summary>
[ShellFeature(
    DisplayName = "Multi-tenant HTTP Routing",
    Description = "Provides multi-tenant HTTP routing capabilities for workflows")]
[UsedImplicitly]
public class MultitenantHttpRoutingFeature : IShellFeature
{
    /// <summary>
    /// Gets or sets the tenant header name.
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

