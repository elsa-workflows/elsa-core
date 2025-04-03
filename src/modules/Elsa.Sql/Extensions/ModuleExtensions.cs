using Elsa.Features.Services;
using Elsa.Sql.Features;

namespace Elsa.Sql.Extensions;

/// <summary>
/// Provides methods to install and configure SQL client features.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Adds the <see cref="SqlFeature"/> feature to the system.
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="configure"></param>
    /// <returns></returns>
    public static IModule UseSql(this IModule configuration, Action<SqlFeature>? configure = default)
    {
        configuration.Configure(configure);
        return configuration;
    }
}