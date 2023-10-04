using Elsa.EntityFrameworkCore.Common;
using Elsa.EntityFrameworkCore.Modules.Alterations;
using Elsa.EntityFrameworkCore.Modules.Management;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

/// <summary>
/// Extends EF Core features.
/// </summary>
public static class ManagementExtensions
{
    /// <summary>
    /// Configures the <see cref="EFCoreWorkflowDefinitionPersistenceFeature"/> to use MySql.
    /// </summary>
    public static EFCoreWorkflowDefinitionPersistenceFeature UseMySql(this EFCoreWorkflowDefinitionPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaMySql(typeof(ManagementExtensions).Assembly, connectionString, options);
        return feature;
    }
    
    /// <summary>
    /// Configures the <see cref="EFCoreWorkflowDefinitionPersistenceFeature"/> to use Sqlite.
    /// </summary>
    public static EFCoreWorkflowDefinitionPersistenceFeature UseSqlite(this EFCoreWorkflowDefinitionPersistenceFeature feature, string connectionString = Constants.DefaultConnectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlite(typeof(ManagementExtensions).Assembly, connectionString, options);
        return feature;
    }
    
    /// <summary>
    /// Configures the <see cref="EFCoreWorkflowDefinitionPersistenceFeature"/> to use SqlServer.
    /// </summary>
    public static EFCoreWorkflowDefinitionPersistenceFeature UseSqlServer(this EFCoreWorkflowDefinitionPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlServer(typeof(ManagementExtensions).Assembly, connectionString, options);
        return feature;
    }
    
    /// <summary>
    /// Configures the <see cref="EFCoreWorkflowDefinitionPersistenceFeature"/> to use SqlServer.
    /// </summary>
    public static EFCoreWorkflowDefinitionPersistenceFeature UsePostgreSql(this EFCoreWorkflowDefinitionPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaPostgreSql(typeof(ManagementExtensions).Assembly, connectionString, options);
        return feature;
    }
    
    /// <summary>
    /// Configures the <see cref="EFCoreWorkflowInstancePersistenceFeature"/> to use MySql.
    /// </summary>
    public static EFCoreWorkflowInstancePersistenceFeature UseMySql(this EFCoreWorkflowInstancePersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaMySql(typeof(ManagementExtensions).Assembly, connectionString, options);
        return feature;
    }
    
    /// <summary>
    /// Configures the <see cref="EFCoreWorkflowInstancePersistenceFeature"/> to use Sqlite.
    /// </summary>
    public static EFCoreWorkflowInstancePersistenceFeature UseSqlite(this EFCoreWorkflowInstancePersistenceFeature feature, string connectionString = Constants.DefaultConnectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlite(typeof(ManagementExtensions).Assembly, connectionString, options);
        return feature;
    }
    
    /// <summary>
    /// Configures the <see cref="EFCoreWorkflowInstancePersistenceFeature"/> to use SqlServer.
    /// </summary>
    public static EFCoreWorkflowInstancePersistenceFeature UseSqlServer(this EFCoreWorkflowInstancePersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlServer(typeof(ManagementExtensions).Assembly, connectionString, options);
        return feature;
    }
    
    /// <summary>
    /// Configures the <see cref="EFCoreWorkflowInstancePersistenceFeature"/> to use SqlServer.
    /// </summary>
    public static EFCoreWorkflowInstancePersistenceFeature UsePostgreSql(this EFCoreWorkflowInstancePersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaPostgreSql(typeof(ManagementExtensions).Assembly, connectionString, options);
        return feature;
    }
    
    /// <summary>
    /// Configures the <see cref="WorkflowManagementPersistenceFeature"/> to use MySql.
    /// </summary>
    public static WorkflowManagementPersistenceFeature UseMySql(this WorkflowManagementPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaMySql(typeof(ManagementExtensions).Assembly, connectionString, options);
        return feature;
    }
    
    /// <summary>
    /// Configures the <see cref="WorkflowManagementPersistenceFeature"/> to use Sqlite.
    /// </summary>
    public static WorkflowManagementPersistenceFeature UseSqlite(this WorkflowManagementPersistenceFeature feature, string connectionString = Constants.DefaultConnectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlite(typeof(ManagementExtensions).Assembly, connectionString, options);
        return feature;
    }
    
    /// <summary>
    /// Configures the <see cref="WorkflowManagementPersistenceFeature"/> to use SqlServer.
    /// </summary>
    public static WorkflowManagementPersistenceFeature UseSqlServer(this WorkflowManagementPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlServer(typeof(ManagementExtensions).Assembly, connectionString, options);
        return feature;
    }
    
    /// <summary>
    /// Configures the <see cref="WorkflowManagementPersistenceFeature"/> to use SqlServer.
    /// </summary>
    public static WorkflowManagementPersistenceFeature UsePostgreSql(this WorkflowManagementPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaPostgreSql(typeof(ManagementExtensions).Assembly, connectionString, options);
        return feature;
    }
}