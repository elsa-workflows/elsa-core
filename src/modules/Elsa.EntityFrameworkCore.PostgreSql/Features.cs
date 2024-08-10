using System.Reflection;
using Elsa.EntityFrameworkCore.Common;
using Elsa.EntityFrameworkCore.Modules.Alterations;
using Elsa.EntityFrameworkCore.Modules.Identity;
using Elsa.EntityFrameworkCore.Modules.Labels;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.EntityFrameworkCore.PostgreSql.Handlers;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

/// <summary>
/// Provides extensions to configure EF Core to use PostgreSQL.
/// </summary>
public static class PostgreSqlProvidersExtensions
{
    private static Assembly Assembly => typeof(PostgreSqlProvidersExtensions).Assembly;
    
    /// <summary>
    /// Configures the <see cref="EFCoreIdentityPersistenceFeature"/> to use PostgreSQL.
    /// </summary>
    public static EFCoreIdentityPersistenceFeature UsePostgreSql(this EFCoreIdentityPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaPostgreSql(Assembly, connectionString, options);
        feature.DbExceptionHandler = _ => new DbExceptionHandler();
        return feature;
    }
    
    /// <summary>
    /// Configures the <see cref="EFCoreAlterationsPersistenceFeature"/> to use PostgreSQL.
    /// </summary>
    public static EFCoreAlterationsPersistenceFeature UsePostgreSql(this EFCoreAlterationsPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaPostgreSql(Assembly, connectionString, options);
        feature.DbExceptionHandler = _ => new DbExceptionHandler();
        return feature;
    }
    
    /// <summary>
    /// Configures the <see cref="EFCoreLabelPersistenceFeature"/> to use PostgreSQL.
    /// </summary>
    public static EFCoreLabelPersistenceFeature UsePostgreSql(this EFCoreLabelPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaPostgreSql(Assembly, connectionString, options);
        feature.DbExceptionHandler = _ => new DbExceptionHandler();
        return feature;
    }
    
    /// <summary>
    /// Configures the <see cref="EFCoreWorkflowDefinitionPersistenceFeature"/> to use PostgreSQL.
    /// </summary>
    public static EFCoreWorkflowDefinitionPersistenceFeature UsePostgreSql(this EFCoreWorkflowDefinitionPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaPostgreSql(Assembly, connectionString, options);
        feature.DbExceptionHandler = _ => new DbExceptionHandler();
        return feature;
    }
    
    /// <summary>
    /// Configures the <see cref="EFCoreWorkflowInstancePersistenceFeature"/> to use PostgreSQL.
    /// </summary>
    public static EFCoreWorkflowInstancePersistenceFeature UsePostgreSql(this EFCoreWorkflowInstancePersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaPostgreSql(Assembly, connectionString, options);
        feature.DbExceptionHandler = _ => new DbExceptionHandler();
        return feature;
    }
    
    /// <summary>
    /// Configures the <see cref="WorkflowManagementPersistenceFeature"/> to use PostgreSQL.
    /// </summary>
    public static WorkflowManagementPersistenceFeature UsePostgreSql(this WorkflowManagementPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaPostgreSql(Assembly, connectionString, options);
        feature.DbExceptionHandler = _ => new DbExceptionHandler();
        return feature;
    }
    
    /// <summary>
    /// Configures the <see cref="EFCoreWorkflowRuntimePersistenceFeature"/> to use PostgreSQL.
    /// </summary>
    public static EFCoreWorkflowRuntimePersistenceFeature UsePostgreSql(this EFCoreWorkflowRuntimePersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaPostgreSql(Assembly, connectionString, options);
        feature.DbExceptionHandler = _ => new DbExceptionHandler();
        return feature;
    }
}