using Elsa.Dapper.Features;
using Elsa.Features.Services;
using FluentMigrator.Runner;
using JetBrains.Annotations;

namespace Elsa.Dapper.Extensions;

/// <summary>
/// Provides extensions to add the Dapper migrations feature.
/// </summary>
[PublicAPI]
public static class DapperMigrationsFeatureExtensions
{
    /// <summary>
    /// Adds the Dapper migrations feature. 
    /// </summary>
    public static IModule UseDapperMigrations(this IModule module, Action<IMigrationRunnerBuilder> configure) => module.UseDapperMigrations(feature => feature.ConfigureRunner = configure);
    
    /// <summary>
    /// Adds the Dapper migrations feature. 
    /// </summary>
    public static IModule UseDapperMigrations(this IModule module, Action<DapperMigrationsFeature>? configure = default)
    {
        module.Configure(configure);
        return module;
    }
}