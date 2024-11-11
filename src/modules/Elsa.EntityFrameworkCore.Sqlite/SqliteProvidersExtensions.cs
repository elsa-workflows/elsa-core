using System.Reflection;
using Elsa.EntityFrameworkCore.EntityHandlers;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

/// <summary>
/// Provides extensions to configure EF Core to use Sqlite.
/// </summary>
public static class SqliteProvidersExtensions
{
    private static Assembly Assembly => typeof(SqliteProvidersExtensions).Assembly;

    /// <summary>
    /// Configures the feature to use Sqlite.
    /// </summary>
    public static TFeature UseSqlite<TFeature, TDbContext>(this PersistenceFeatureBase<TFeature, TDbContext> feature, 
        string? connectionString = null, 
        ElsaDbContextOptions? options = null,
        Action<SqliteDbContextOptionsBuilder>? configure = null)
        where TDbContext : ElsaDbContextBase
        where TFeature : PersistenceFeatureBase<TFeature, TDbContext>
    {
        return feature.UseSqlite(Assembly, connectionString, options, configure);
    }

    /// <summary>
    /// Configures the feature to use Sqlite.
    /// </summary>
    public static TFeature UseSqlite<TFeature, TDbContext>(this PersistenceFeatureBase<TFeature, TDbContext> feature, 
        Func<IServiceProvider, string> connectionStringFunc, 
        ElsaDbContextOptions? options = null,
        Action<SqliteDbContextOptionsBuilder>? configure = null)
        where TDbContext : ElsaDbContextBase
        where TFeature : PersistenceFeatureBase<TFeature, TDbContext>
    {
        return feature.UseSqlite(Assembly, connectionStringFunc, options, configure);
    }

    /// <summary>
    /// Configures the feature to use Sqlite.
    /// </summary>
    public static TFeature UseSqlite<TFeature, TDbContext>(this PersistenceFeatureBase<TFeature, TDbContext> feature, 
        Assembly migrationsAssembly, 
        string? connectionString = null, 
        ElsaDbContextOptions? options = null,
        Action<SqliteDbContextOptionsBuilder>? configure = null)
        where TDbContext : ElsaDbContextBase
        where TFeature : PersistenceFeatureBase<TFeature, TDbContext>
    {
        connectionString ??= "Data Source=elsa.sqlite.db;Cache=Shared;";
        return feature.UseSqlite(migrationsAssembly, _ => connectionString, options, configure);
    }

    /// <summary>
    /// Configures the feature to use Sqlite.
    /// </summary>
    public static TFeature UseSqlite<TFeature, TDbContext>(this PersistenceFeatureBase<TFeature, TDbContext> feature, 
        Assembly migrationsAssembly, Func<IServiceProvider, string> connectionStringFunc, 
        ElsaDbContextOptions? options = null,
        Action<SqliteDbContextOptionsBuilder>? configure = null)
        where TDbContext : ElsaDbContextBase
        where TFeature : PersistenceFeatureBase<TFeature, TDbContext>
    {
        feature.Module.Services.AddScoped<IEntityModelCreatingHandler, SetupForSqlite>();
        feature.DbContextOptionsBuilder = (sp, db) => db.UseElsaSqlite(migrationsAssembly, connectionStringFunc(sp), options, configure: configure);
        return (TFeature)feature;
    }
}