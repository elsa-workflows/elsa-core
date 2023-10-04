using Elsa.EntityFrameworkCore.Common;
using Elsa.EntityFrameworkCore.Modules.Alterations;
using Elsa.EntityFrameworkCore.Modules.Runtime;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

/// <summary>
/// Extends EF Core features.
/// </summary>
public static class RuntimeExtensions
{
    /// <summary>
    /// Configures the <see cref="EFCoreAlterationPersistenceFeature"/> to use MySql.
    /// </summary>
    public static EFCoreWorkflowRuntimePersistenceFeature UseMySql(this EFCoreWorkflowRuntimePersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaMySql(typeof(RuntimeExtensions).Assembly, connectionString, options);
        return feature;
    }
    
    /// <summary>
    /// Configures the <see cref="EFCoreAlterationPersistenceFeature"/> to use Sqlite.
    /// </summary>
    public static EFCoreWorkflowRuntimePersistenceFeature UseSqlite(this EFCoreWorkflowRuntimePersistenceFeature feature, string connectionString = Constants.DefaultConnectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlite(typeof(RuntimeExtensions).Assembly, connectionString, options);
        return feature;
    }
    
    /// <summary>
    /// Configures the <see cref="EFCoreAlterationPersistenceFeature"/> to use SqlServer.
    /// </summary>
    public static EFCoreWorkflowRuntimePersistenceFeature UseSqlServer(this EFCoreWorkflowRuntimePersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlServer(typeof(RuntimeExtensions).Assembly, connectionString, options);
        return feature;
    }
    
    /// <summary>
    /// Configures the <see cref="EFCoreAlterationPersistenceFeature"/> to use SqlServer.
    /// </summary>
    public static EFCoreWorkflowRuntimePersistenceFeature UsePostgreSql(this EFCoreWorkflowRuntimePersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaPostgreSql(typeof(RuntimeExtensions).Assembly, connectionString, options);
        return feature;
    }
}