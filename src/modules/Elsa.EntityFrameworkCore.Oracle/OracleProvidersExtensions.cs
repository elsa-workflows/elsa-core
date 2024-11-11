using System.Reflection;
using Oracle.EntityFrameworkCore.Infrastructure;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

/// <summary>
/// Provides extensions to configure EF Core to use Oracle.
/// </summary>
public static class OracleProvidersExtensions
{
    private static Assembly Assembly => typeof(OracleProvidersExtensions).Assembly;
    
    public static TFeature UseMySql<TFeature, TDbContext>(this PersistenceFeatureBase<TFeature, TDbContext> feature, 
        string connectionString, 
        ElsaDbContextOptions? options = null, 
        Action<OracleDbContextOptionsBuilder>? configure = null) 
        where TDbContext : ElsaDbContextBase
        where TFeature : PersistenceFeatureBase<TFeature, TDbContext>
    {
        return feature.UseMySql(Assembly, connectionString, options, configure);
    }
    
    public static TFeature UseMySql<TFeature, TDbContext>(this PersistenceFeatureBase<TFeature, TDbContext> feature, 
        Func<IServiceProvider, string> connectionStringFunc,
        ElsaDbContextOptions? options = null,
        Action<OracleDbContextOptionsBuilder>? configure = null
    ) 
        where TDbContext : ElsaDbContextBase
        where TFeature : PersistenceFeatureBase<TFeature, TDbContext>
    {
        return feature.UseMySql(Assembly, connectionStringFunc, options, configure);
    }
    
    public static TFeature UseMySql<TFeature, TDbContext>(this PersistenceFeatureBase<TFeature, TDbContext> feature, 
        Assembly migrationsAssembly, 
        string connectionString,
        ElsaDbContextOptions? options = null,
        Action<OracleDbContextOptionsBuilder>? configure = null
    ) 
        where TDbContext : ElsaDbContextBase
        where TFeature : PersistenceFeatureBase<TFeature, TDbContext>
    {
        return feature.UseMySql(migrationsAssembly, _ => connectionString, options, configure);
    }
    
    public static TFeature UseMySql<TFeature, TDbContext>(this PersistenceFeatureBase<TFeature, TDbContext> feature, 
        Assembly migrationsAssembly, 
        Func<IServiceProvider, string> connectionStringFunc,
        ElsaDbContextOptions? options = null,
        Action<OracleDbContextOptionsBuilder>? configure = null
    ) 
        where TDbContext : ElsaDbContextBase
        where TFeature : PersistenceFeatureBase<TFeature, TDbContext>
    {
        feature.DbContextOptionsBuilder = (sp, db) => db.UseElsaOracle(migrationsAssembly, connectionStringFunc(sp), options, configure: configure);
        return (TFeature)feature;
    }
}