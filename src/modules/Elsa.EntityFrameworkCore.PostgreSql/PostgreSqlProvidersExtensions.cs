using System.Reflection;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

/// <summary>
/// Provides extensions to configure EF Core to use PostgreSQL.
/// </summary>
public static class PostgreSqlProvidersExtensions
{
    private static Assembly Assembly => typeof(PostgreSqlProvidersExtensions).Assembly;
    
    public static TFeature UsePostgreSql<TFeature, TDbContext>(this PersistenceFeatureBase<TFeature, TDbContext> feature, string connectionString, ElsaDbContextOptions? options = null) 
        where TDbContext : ElsaDbContextBase
        where TFeature : PersistenceFeatureBase<TFeature, TDbContext>
    {
        return feature.UsePostgreSql(Assembly, connectionString, options);
    }
    
    public static TFeature UsePostgreSql<TFeature, TDbContext>(this PersistenceFeatureBase<TFeature, TDbContext> feature, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options = null) 
        where TDbContext : ElsaDbContextBase
        where TFeature : PersistenceFeatureBase<TFeature, TDbContext>
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaPostgreSql(migrationsAssembly, connectionString, options);
        return (TFeature)feature;
    }
}