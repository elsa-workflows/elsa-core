using Elsa.Common.Multitenancy;
using Elsa.Tenants;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EFCore.Modules.Tenants;

/// <summary>
/// Base class for tenant management persistence features.
/// This is not a standalone shell feature - use provider-specific features.
/// </summary>
[UsedImplicitly]
public abstract class EFCoreTenantManagementShellFeatureBase : PersistenceShellFeatureBase<TenantsElsaDbContext>
{
    protected override void OnConfiguring(IServiceCollection services)
    {
        services.AddScoped<ITenantStore, EFCoreTenantStore>();
        AddEntityStore<Tenant, EFCoreTenantStore>(services);
    }
}
