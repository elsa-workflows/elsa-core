using System.Reflection;
using Elsa.EntityFrameworkCore.Common;
using Elsa.EntityFrameworkCore.Modules.Alterations;
using Elsa.EntityFrameworkCore.Modules.Identity;
using Elsa.EntityFrameworkCore.Modules.Labels;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

/// <summary>
/// Provides extensions to configure EF Core to use Oracle.
/// </summary>
public static class OracleProvidersExtensions
{
    private static Assembly Assembly => typeof(OracleProvidersExtensions).Assembly;
    
    /// <summary>
    /// Configures the <see cref="EFCoreIdentityPersistenceFeature"/> to use Oracle.
    /// </summary>
    public static EFCoreIdentityPersistenceFeature UseOracle(this EFCoreIdentityPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaOracle(Assembly, connectionString, options);
        return feature;
    }
    
    /// <summary>
    /// Configures the <see cref="EFCoreAlterationsPersistenceFeature"/> to use Oracle.
    /// </summary>
    public static EFCoreAlterationsPersistenceFeature UseOracle(this EFCoreAlterationsPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaOracle(Assembly, connectionString, options);
        return feature;
    }
    
    /// <summary>
    /// Configures the <see cref="EFCoreLabelPersistenceFeature"/> to use Oracle.
    /// </summary>
    public static EFCoreLabelPersistenceFeature UseOracle(this EFCoreLabelPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaOracle(Assembly, connectionString, options);
        return feature;
    }
    
    /// <summary>
    /// Configures the <see cref="EFCoreWorkflowDefinitionPersistenceFeature"/> to use Oracle.
    /// </summary>
    public static EFCoreWorkflowDefinitionPersistenceFeature UseOracle(this EFCoreWorkflowDefinitionPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaOracle(Assembly, connectionString, options);
        return feature;
    }
    
    /// <summary>
    /// Configures the <see cref="EFCoreWorkflowInstancePersistenceFeature"/> to use Oracle.
    /// </summary>
    public static EFCoreWorkflowInstancePersistenceFeature UseOracle(this EFCoreWorkflowInstancePersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaOracle(Assembly, connectionString, options);
        return feature;
    }
    
    /// <summary>
    /// Configures the <see cref="WorkflowManagementPersistenceFeature"/> to use Oracle.
    /// </summary>
    public static WorkflowManagementPersistenceFeature UseOracle(this WorkflowManagementPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaOracle(Assembly, connectionString, options);
        return feature;
    }
    
    /// <summary>
    /// Configures the <see cref="EFCoreWorkflowRuntimePersistenceFeature"/> to use Oracle.
    /// </summary>
    public static EFCoreWorkflowRuntimePersistenceFeature UseOracle(this EFCoreWorkflowRuntimePersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaOracle(Assembly, connectionString, options);
        return feature;
    }
}