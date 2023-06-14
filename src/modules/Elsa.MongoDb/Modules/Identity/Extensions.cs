using Elsa.Identity.Features;

namespace Elsa.MongoDb.Modules.Identity;

/// <summary>
/// Provides extensions for <see cref="IdentityFeature"/> 
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Sets up the MongoDB persistence provider for the User, Application and Role stores. 
    /// </summary>
    public static IdentityFeature UseMongoDb(this IdentityFeature feature, Action<MongoIdentityPersistenceFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}