using System.Reflection;
using Elsa.EntityFrameworkCore.Common;
using Elsa.EntityFrameworkCore.Modules.Alterations;
using Elsa.EntityFrameworkCore.Modules.Identity;
using Elsa.EntityFrameworkCore.Modules.Labels;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Microsoft.EntityFrameworkCore;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

/// <summary>
/// Provides extensions to configure EF Core to use MySQL.
/// </summary>
public static class MySqlProvidersExtensions
{
    private static Assembly Assembly => typeof(MySqlProvidersExtensions).Assembly;
    
    /// <summary>
    /// Configures the <see cref="EFCoreIdentityPersistenceFeature"/> to use MySql.
    /// </summary>
    public static EFCoreIdentityPersistenceFeature UseMySql(this EFCoreIdentityPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default, ServerVersion? serverVersion = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaMySql(Assembly, connectionString, options, serverVersion);
        return feature;
    }
    
    /// <summary>
    /// Configures the <see cref="EFCoreAlterationsPersistenceFeature"/> to use MySql.
    /// </summary>
    public static EFCoreAlterationsPersistenceFeature UseMySql(this EFCoreAlterationsPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default, ServerVersion? serverVersion = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaMySql(Assembly, connectionString, options, serverVersion);
        return feature;
    }
    
    /// <summary>
    /// Configures the <see cref="EFCoreLabelPersistenceFeature"/> to use MySql.
    /// </summary>
    public static EFCoreLabelPersistenceFeature UseMySql(this EFCoreLabelPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default, ServerVersion? serverVersion = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaMySql(Assembly, connectionString, options, serverVersion);
        return feature;
    }
    
    /// <summary>
    /// Configures the <see cref="EFCoreWorkflowDefinitionPersistenceFeature"/> to use MySql.
    /// </summary>
    public static EFCoreWorkflowDefinitionPersistenceFeature UseMySql(this EFCoreWorkflowDefinitionPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default, ServerVersion? serverVersion = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaMySql(Assembly, connectionString, options, serverVersion);
        return feature;
    }
    
    /// <summary>
    /// Configures the <see cref="EFCoreWorkflowInstancePersistenceFeature"/> to use MySql.
    /// </summary>
    public static EFCoreWorkflowInstancePersistenceFeature UseMySql(this EFCoreWorkflowInstancePersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default, ServerVersion? serverVersion = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaMySql(Assembly, connectionString, options, serverVersion);
        return feature;
    }
    
    /// <summary>
    /// Configures the <see cref="WorkflowManagementPersistenceFeature"/> to use MySql.
    /// </summary>
    public static WorkflowManagementPersistenceFeature UseMySql(this WorkflowManagementPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default, ServerVersion? serverVersion = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaMySql(Assembly, connectionString, options, serverVersion);
        return feature;
    }
    
    /// <summary>
    /// Configures the <see cref="EFCoreWorkflowRuntimePersistenceFeature"/> to use MySql.
    /// </summary>
    public static EFCoreWorkflowRuntimePersistenceFeature UseMySql(this EFCoreWorkflowRuntimePersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default, ServerVersion? serverVersion = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaMySql(Assembly, connectionString, options, serverVersion);
        return feature;
    }
}