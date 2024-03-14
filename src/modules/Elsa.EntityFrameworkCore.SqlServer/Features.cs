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
/// Provides extensions to configure EF Core to use SQL Server.
/// </summary>
public static class SqlServerProvidersExtensions
{
    private static Assembly Assembly => typeof(SqlServerProvidersExtensions).Assembly;

    /// <summary>
    /// Configures the <see cref="EFCoreIdentityPersistenceFeature"/> to use SQL Server.
    /// </summary>
    public static EFCoreIdentityPersistenceFeature UseSqlServer(this EFCoreIdentityPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlServer(Assembly, connectionString, options);
        return feature;
    }

    /// <summary>
    /// Configures the <see cref="EFCoreAlterationsPersistenceFeature"/> to use SQL Server.
    /// </summary>
    public static EFCoreAlterationsPersistenceFeature UseSqlServer(this EFCoreAlterationsPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlServer(Assembly, connectionString, options);
        return feature;
    }

    /// <summary>
    /// Configures the <see cref="EFCoreLabelPersistenceFeature"/> to use SQL Server.
    /// </summary>
    public static EFCoreLabelPersistenceFeature UseSqlServer(this EFCoreLabelPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlServer(Assembly, connectionString, options);
        return feature;
    }

    /// <summary>
    /// Configures the <see cref="EFCoreWorkflowDefinitionPersistenceFeature"/> to use SQL Server.
    /// </summary>
    public static EFCoreWorkflowDefinitionPersistenceFeature UseSqlServer(this EFCoreWorkflowDefinitionPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlServer(Assembly, connectionString, options);
        return feature;
    }

    /// <summary>
    /// Configures the <see cref="EFCoreWorkflowInstancePersistenceFeature"/> to use SQL Server.
    /// </summary>
    public static EFCoreWorkflowInstancePersistenceFeature UseSqlServer(this EFCoreWorkflowInstancePersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlServer(Assembly, connectionString, options);
        return feature;
    }

    /// <summary>
    /// Configures the <see cref="WorkflowManagementPersistenceFeature"/> to use SQL Server.
    /// </summary>
    public static WorkflowManagementPersistenceFeature UseSqlServer(this WorkflowManagementPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlServer(Assembly, connectionString, options);
        return feature;
    }

    /// <summary>
    /// Configures the <see cref="EFCoreWorkflowRuntimePersistenceFeature"/> to use SQL Server.
    /// </summary>
    public static EFCoreWorkflowRuntimePersistenceFeature UseSqlServer(this EFCoreWorkflowRuntimePersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlServer(Assembly, connectionString, options);
        return feature;
    }
}