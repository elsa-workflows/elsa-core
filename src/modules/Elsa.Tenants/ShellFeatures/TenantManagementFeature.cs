using CShells.Features;
using Elsa.Common.Multitenancy;
using Elsa.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Tenants.ShellFeatures;

/// <summary>
/// Enables tenant management features.
/// </summary>
[ShellFeature(
    DisplayName = "Tenant Management",
    Description = "Provides tenant storage and management capabilities")]
public class TenantManagementFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddMemoryStore<Tenant, MemoryTenantStore>()
            .AddScoped<ITenantStore, MemoryTenantStore>();
    }
}
