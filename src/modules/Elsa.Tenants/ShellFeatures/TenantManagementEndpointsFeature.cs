using CShells.Features;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Tenants.ShellFeatures;

/// <summary>
/// Enables tenant management endpoints.
/// </summary>
[ShellFeature(
    DisplayName = "Tenant Management Endpoints",
    Description = "Provides REST API endpoints for tenant management",
    DependsOn = ["TenantManagement"])]
[UsedImplicitly]
public class TenantManagementEndpointsFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        // FastEndpoints are registered via assembly scanning
    }
}
