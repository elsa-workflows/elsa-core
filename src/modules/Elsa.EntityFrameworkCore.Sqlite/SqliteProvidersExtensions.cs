using System.Reflection;
using Elsa.EntityFrameworkCore.Contracts;
using Elsa.EntityFrameworkCore.EntityHandlers;
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
    public static TFeature UseSqlite<TFeature, TDbContext>(this PersistenceFeatureBase<TFeature, TDbContext> feature, string? connectionString = null, ElsaDbContextOptions? options = null) 
        where TDbContext : ElsaDbContextBase
        where TFeature : PersistenceFeatureBase<TFeature, TDbContext>
    {
        return feature.UseSqlite(Assembly, connectionString, options);
    }
    
    /// <summary>
    /// Configures the feature to use Sqlite.
    /// </summary>
    public static TFeature UseSqlite<TFeature, TDbContext>(this PersistenceFeatureBase<TFeature, TDbContext> feature, Func<IServiceProvider,string> connectionStringFunc, ElsaDbContextOptions? options = null) 
        where TDbContext : ElsaDbContextBase
        where TFeature : PersistenceFeatureBase<TFeature, TDbContext>
    {
        return feature.UseSqlite(Assembly, connectionStringFunc, options);
    }
    
    /// <summary>
    /// Configures the feature to use Sqlite.
    /// </summary>
    public static TFeature UseSqlite<TFeature, TDbContext>(this PersistenceFeatureBase<TFeature, TDbContext> feature, Assembly migrationsAssembly, string? connectionString = null, ElsaDbContextOptions? options = null) 
        where TDbContext : ElsaDbContextBase
        where TFeature : PersistenceFeatureBase<TFeature, TDbContext>
    {
        connectionString ??= "Data Source=elsa.sqlite.db;Cache=Shared;";
        return feature.UseSqlite(migrationsAssembly, _ => connectionString, options);
    }
    
    /// <summary>
    /// Configures the feature to use Sqlite.
    /// </summary>
    public static TFeature UseSqlite<TFeature, TDbContext>(this PersistenceFeatureBase<TFeature, TDbContext> feature, Assembly migrationsAssembly, Func<IServiceProvider, string> connectionStringFunc, ElsaDbContextOptions? options = null) 
        where TDbContext : ElsaDbContextBase
        where TFeature : PersistenceFeatureBase<TFeature, TDbContext>
    {
        feature.Module.Services.AddScoped<IEntityModelCreatingHandler, SetupForSqlite>();
        feature.DbContextOptionsBuilder = (sp, db) => db.UseElsaSqlite(migrationsAssembly, connectionStringFunc(sp), options);
        return (TFeature)feature;
    }
}