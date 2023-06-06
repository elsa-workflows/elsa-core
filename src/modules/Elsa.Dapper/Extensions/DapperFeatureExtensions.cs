using Elsa.Dapper.Features;
using Elsa.Features.Services;
using JetBrains.Annotations;

namespace Elsa.Dapper.Extensions;

/// <summary>
/// Provides extensions to add the Dapper feature.
/// </summary>
[PublicAPI]
public static class DapperFeatureExtensions
{
    /// <summary>
    /// Adds the Dapper migrations feature. 
    /// </summary>
    public static IModule UseDapper(this IModule module, Action<DapperFeature>? configure = default)
    {
        module.Configure(configure);
        return module;
    }
}