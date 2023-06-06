using Elsa.Dapper.Modules.Identity.Features;
using Elsa.Identity.Features;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extensions for <see cref="IdentityFeature"/> 
/// </summary>
public static class IdentityFeatureExtensions
{
    /// <summary>
    /// Sets up the EF Core persistence provider for the User, Application and Role stores. 
    /// </summary>
    public static IdentityFeature UseDapper(this IdentityFeature feature, Action<DapperIdentityPersistenceFeature>? configure = default)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}