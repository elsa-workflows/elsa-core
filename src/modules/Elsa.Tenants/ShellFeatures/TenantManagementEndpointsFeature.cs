using CShells.FastEndpoints.Features;
using CShells.Features;

namespace Elsa.Tenants.ShellFeatures;

/// <summary>
/// Enables tenant management endpoints.
/// </summary>
[ShellFeature(
    DisplayName = "Tenant Management Endpoints",
    Description = "Provides REST API endpoints for tenant management",
    DependsOn = ["TenantManagement"])]
public class TenantManagementEndpointsFeature : IFastEndpointsShellFeature
{
    public void ConfigureServices(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
    {
        // Endpoints are registered automatically via IFastEndpointsShellFeature
    }
}
