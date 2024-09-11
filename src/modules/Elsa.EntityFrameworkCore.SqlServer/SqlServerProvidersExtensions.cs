using System.Reflection;
using Elsa.EntityFrameworkCore.Common;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

/// <summary>
/// Provides extensions to configure EF Core to use SQL Server.
/// </summary>
public static class SqlServerProvidersExtensions
{
    private static Assembly Assembly => typeof(SqlServerProvidersExtensions).Assembly;
    
    public static TFeature UseSqlServer<TFeature, TDbContext>(this PersistenceFeatureBase<TFeature, TDbContext> feature, string connectionString, ElsaDbContextOptions? options = null) 
        where TDbContext : ElsaDbContextBase
        where TFeature : PersistenceFeatureBase<TFeature, TDbContext>
    {
        return feature.UseSqlServer(Assembly, connectionString, options);
    }
    
    public static TFeature UseSqlServer<TFeature, TDbContext>(this PersistenceFeatureBase<TFeature, TDbContext> feature, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options = null) 
        where TDbContext : ElsaDbContextBase
        where TFeature : PersistenceFeatureBase<TFeature, TDbContext>
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlServer(Assembly, connectionString, options);
        return (TFeature)feature;
    }
}