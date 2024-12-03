using Elsa.Common.Multitenancy;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.MongoDb.Common;
using Elsa.Tenants.Features;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.MongoDb.Modules.Tenants;

/// <summary>
/// Configures the <see cref="TenantManagementFeature"/> feature with MongoDB persistence providers.
/// </summary>
[DependsOn(typeof(TenantManagementFeature))]
[UsedImplicitly]
public class MongoTenantsPersistenceFeature(IModule module) : PersistenceFeatureBase(module)
{
    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<TenantManagementFeature>(feature =>
        {
            feature.WithTenantStore(sp => sp.GetRequiredService<MongoTenantStore>());
        });
    }

    /// <inheritdoc />
    public override void Apply()
    {
        base.Apply();
        
        AddCollection<Tenant>("tenants");
        AddStore<Tenant, MongoTenantStore>();

        Services.AddHostedService<CreateIndices>();
    }
}