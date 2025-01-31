using System.Reflection;
using Elsa.EntityFrameworkCore.Modules.Alterations;
using Elsa.EntityFrameworkCore.Oracle;
using Elsa.Extensions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Oracle.EntityFrameworkCore.Infrastructure;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

/// <summary>
/// Provides extensions to configure EF Core to use Oracle.
/// </summary>
[PublicAPI]
public static class OracleProvidersExtensions
{
    private static Assembly Assembly => typeof(OracleProvidersExtensions).Assembly;

    public static TFeature UseOracle<TFeature, TDbContext>(this PersistenceFeatureBase<TFeature, TDbContext> feature,
        string connectionString,
        ElsaDbContextOptions? options = null,
        Action<OracleDbContextOptionsBuilder>? configure = null)
        where TDbContext : ElsaDbContextBase
        where TFeature : PersistenceFeatureBase<TFeature, TDbContext>
    {
        return feature.UseOracle(Assembly, connectionString, options, configure);
    }

    public static TFeature UseOracle<TFeature, TDbContext>(this PersistenceFeatureBase<TFeature, TDbContext> feature,
        Func<IServiceProvider, string> connectionStringFunc,
        ElsaDbContextOptions? options = null,
        Action<OracleDbContextOptionsBuilder>? configure = null
    )
        where TDbContext : ElsaDbContextBase
        where TFeature : PersistenceFeatureBase<TFeature, TDbContext>
    {
        return feature.UseOracle(Assembly, connectionStringFunc, options, configure);
    }

    public static TFeature UseOracle<TFeature, TDbContext>(this PersistenceFeatureBase<TFeature, TDbContext> feature,
        Assembly migrationsAssembly,
        string connectionString,
        ElsaDbContextOptions? options = null,
        Action<OracleDbContextOptionsBuilder>? configure = null
    )
        where TDbContext : ElsaDbContextBase
        where TFeature : PersistenceFeatureBase<TFeature, TDbContext>
    {
        return feature.UseOracle(migrationsAssembly, _ => connectionString, options, configure);
    }

    public static TFeature UseOracle<TFeature, TDbContext>(this PersistenceFeatureBase<TFeature, TDbContext> feature,
        Assembly migrationsAssembly,
        Func<IServiceProvider, string> connectionStringFunc,
        ElsaDbContextOptions? options = null,
        Action<OracleDbContextOptionsBuilder>? configure = null
    )
        where TDbContext : ElsaDbContextBase
        where TFeature : PersistenceFeatureBase<TFeature, TDbContext>
    {
        feature.Services.TryAddScopedImplementation<IEntityModelCreatingHandler, SetupForAlterations>();
        feature.Services.TryAddScopedImplementation<IEntityModelCreatingHandler, SetupForManagement>();
        feature.Services.TryAddScopedImplementation<IEntityModelCreatingHandler, SetupForRuntime>();

        feature.DbContextOptionsBuilder = (sp, db) => db.UseElsaOracle(migrationsAssembly, connectionStringFunc(sp), options, configure: configure);
        return (TFeature)feature;
    }
}