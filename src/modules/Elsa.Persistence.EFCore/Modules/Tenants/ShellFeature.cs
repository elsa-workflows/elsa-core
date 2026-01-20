using CShells.Features;
using Elsa.Common.Multitenancy;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EFCore.Modules.Tenants;

/// <summary>
/// Configures the tenant management feature with an Entity Framework Core persistence provider.
/// </summary>
[ShellFeature(
    DisplayName = "EF Core Tenant Management Persistence",
    Description = "Provides Entity Framework Core persistence for tenant management",
    DependsOn = ["TenantManagement"])]
[UsedImplicitly]
public class EFCoreTenantManagementShellFeature : PersistenceShellFeatureBase<EFCoreTenantManagementShellFeature, TenantsElsaDbContext>
{
    protected override void OnConfiguring(IServiceCollection services)
    {
        AddEntityStore<Tenant, EFCoreTenantStore>(services);
    }
}
