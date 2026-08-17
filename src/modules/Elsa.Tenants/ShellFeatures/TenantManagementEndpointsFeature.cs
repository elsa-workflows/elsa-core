using CShells.Features;
using Elsa.Platform.PackageManifest.Generator.Hints;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Tenants.ShellFeatures;

/// <summary>
/// Enables tenant management endpoints.
/// </summary>
[ManifestFeatureCategory("Tenancy")]
[ManifestFeatureCategory("API")]
[ShellFeature(
    DisplayName = "Tenant Management Endpoints",
    Description = "Provides REST API endpoints for tenant management",
    DependsOn = [typeof(TenantManagementFeature)])]
[UsedImplicitly]
public class TenantManagementEndpointsFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        // FastEndpoints are registered via assembly scanning
    }
}
