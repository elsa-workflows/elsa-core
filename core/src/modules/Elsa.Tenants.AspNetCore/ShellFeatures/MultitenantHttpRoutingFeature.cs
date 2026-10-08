using CShells.Features;
using Elsa.Common.Multitenancy;
using Elsa.Tenants.AspNetCore.Options;
using Elsa.Tenants.AspNetCore.Services;
using Elsa.Platform.PackageManifest.Generator.Hints;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Tenants.AspNetCore.ShellFeatures;

/// <summary>
/// Provides multi-tenant HTTP routing features.
/// </summary>
[ManifestFeatureCategory("Tenancy")]
[ManifestFeatureCategory("HTTP")]
[ShellFeature(
    DisplayName = "Multi-tenant HTTP Routing",
    Description = "Provides multi-tenant HTTP routing capabilities for workflows")]
[UsedImplicitly]
public class MultitenantHttpRoutingFeature : IShellFeature
{
    /// <summary>
    /// Gets or sets the tenant header name.
    /// </summary>
    [ManifestSetting(
        DisplayName = "Tenant Header Name",
        Description = "HTTP header used to resolve the current tenant.",
        Category = "Routing",
        DefaultValue = "X-Tenant-Id",
        RestartRequired = true)]
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
