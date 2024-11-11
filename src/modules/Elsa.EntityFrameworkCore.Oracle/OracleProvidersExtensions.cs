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
        Assembly migrationsAssembly, 
        string connectionString, 
        ElsaDbContextOptions? options = null,
        Action<OracleDbContextOptionsBuilder>? configure = null
    ) 
        where TDbContext : ElsaDbContextBase
        where TFeature : PersistenceFeatureBase<TFeature, TDbContext>
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaOracle(Assembly, connectionString, options, configure: configure);
        return (TFeature)feature;
    }
}