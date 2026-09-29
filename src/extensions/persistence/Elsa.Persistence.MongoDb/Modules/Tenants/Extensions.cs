using Elsa.Identity.Features;
using Elsa.Tenants.Features;

namespace Elsa.Persistence.MongoDb.Modules.Tenants;

/// <summary>
/// Provides extensions for <see cref="IdentityFeature"/> 
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Sets up the MongoDB persistence provider for the User, Application and Role stores. 
    /// </summary>
    public static TenantManagementFeature UseMongoDb(this TenantManagementFeature feature, Action<MongoTenantsPersistenceFeature>? configure = null)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}