using CShells.Features;
using Elsa.Common.Multitenancy;
using Elsa.Extensions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Tenants.ShellFeatures;

/// <summary>
/// Enables tenant management capabilities.
/// </summary>
[ShellFeature(
    DisplayName = "Tenant Management",
    Description = "Provides tenant store and management capabilities")]
[UsedImplicitly]
public class TenantManagementFeature : IShellFeature
{
    /// <summary>
    /// A factory that instantiates an <see cref="ITenantStore"/>.
    /// </summary>
    public Func<IServiceProvider, ITenantStore> TenantStoreFactory { get; set; } = sp => sp.GetRequiredService<MemoryTenantStore>();

    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddMemoryStore<Tenant, MemoryTenantStore>()
            .AddScoped(TenantStoreFactory);
    }
}

