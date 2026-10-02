using Elsa.Persistence.Dapper.Features;
using Elsa.Features.Services;
using JetBrains.Annotations;

namespace Elsa.Persistence.Dapper.Extensions;

/// <summary>
/// Provides extensions to add the Dapper feature.
/// </summary>
[PublicAPI]
public static class DapperFeatureExtensions
{
    /// <summary>
    /// Adds the Dapper migrations feature. 
    /// </summary>
    public static IModule UseDapper(this IModule module, Action<DapperFeature>? configure = null)
    {
        module.Configure(configure);
        return module;
    }
}