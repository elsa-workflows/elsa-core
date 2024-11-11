using System.Reflection;
using Microsoft.EntityFrameworkCore.Infrastructure;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

/// <summary>
/// Provides extensions to configure EF Core to use SQL Server.
/// </summary>
public static class SqlServerProvidersExtensions
{
    private static Assembly Assembly => typeof(SqlServerProvidersExtensions).Assembly;
    
    public static TFeature UseSqlServer<TFeature, TDbContext>(this PersistenceFeatureBase<TFeature, TDbContext> feature, 
        string connectionString, 
        ElsaDbContextOptions? options = null,
        Action<SqlServerDbContextOptionsBuilder>? configure = null) 
        where TDbContext : ElsaDbContextBase
        where TFeature : PersistenceFeatureBase<TFeature, TDbContext>
    {
        return feature.UseSqlServer(Assembly, connectionString, options, configure);
    }
    
    public static TFeature UseSqlServer<TFeature, TDbContext>(this PersistenceFeatureBase<TFeature, TDbContext> feature, 
        Func<IServiceProvider, string> connectionStringFunc, 
        ElsaDbContextOptions? options = null,
        Action<SqlServerDbContextOptionsBuilder>? configure = null)
        where TDbContext : ElsaDbContextBase
        where TFeature : PersistenceFeatureBase<TFeature, TDbContext>
    {
        return feature.UseSqlServer(Assembly, connectionStringFunc, options, configure);
    }
    
    public static TFeature UseSqlServer<TFeature, TDbContext>(this PersistenceFeatureBase<TFeature, TDbContext> feature, 
        Assembly migrationsAssembly, 
        string connectionString,
        ElsaDbContextOptions? options = null,
        Action<SqlServerDbContextOptionsBuilder>? configure = null) 
        where TDbContext : ElsaDbContextBase
        where TFeature : PersistenceFeatureBase<TFeature, TDbContext>
    {
        return feature.UseSqlServer(migrationsAssembly, _ => connectionString, options, configure);
    }
    
    public static TFeature UseSqlServer<TFeature, TDbContext>(this PersistenceFeatureBase<TFeature, TDbContext> feature, 
        Assembly migrationsAssembly, 
        Func<IServiceProvider, string> connectionStringFunc,
        ElsaDbContextOptions? options = null,
        Action<SqlServerDbContextOptionsBuilder>? configure = null) 
        where TDbContext : ElsaDbContextBase
        where TFeature : PersistenceFeatureBase<TFeature, TDbContext>
    {
        feature.DbContextOptionsBuilder = (sp, db) => db.UseElsaSqlServer(migrationsAssembly, connectionStringFunc(sp), options, configure);
        return (TFeature)feature;
    }
}