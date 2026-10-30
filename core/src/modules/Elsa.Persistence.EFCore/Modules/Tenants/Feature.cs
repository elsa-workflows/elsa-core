using Elsa.Common.Multitenancy;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Tenants.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EFCore.Modules.Tenants;

/// <summary>
/// Configures the <see cref="TenantManagementFeature"/> feature with an Entity Framework Core persistence provider.
/// </summary>
[DependsOn(typeof(TenantManagementFeature))]
public class EFCoreTenantManagementFeature(IModule module) : PersistenceFeatureBase<EFCoreTenantManagementFeature, TenantsElsaDbContext>(module)
{
    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<TenantManagementFeature>(feature =>
        {
            feature.WithTenantStore(sp => sp.GetRequiredService<EFCoreTenantStore>());
        });
    }

    /// <inheritdoc />
    public override void Apply()
    {
        base.Apply();
        AddEntityStore<Tenant, EFCoreTenantStore>();
    }

    protected override void ConfigureMigrations()
    {
        Module.ConfigureHostedService<RunMigrationsHostedService<TenantsElsaDbContext>>(-100);
    }
}