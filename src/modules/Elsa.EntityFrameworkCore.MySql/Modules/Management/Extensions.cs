using Elsa.EntityFrameworkCore.Modules.Management;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

/// <summary>
/// Extends EF Core features.
/// </summary>
[PublicAPI]
public static partial class Extensions
{
    public static EFCoreWorkflowDefinitionPersistenceFeature UseMySql(this EFCoreWorkflowDefinitionPersistenceFeature feature, string connectionString)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaMySql(connectionString);
        return feature;
    }

    public static EFCoreWorkflowInstancePersistenceFeature UseMySql(this EFCoreWorkflowInstancePersistenceFeature feature, string connectionString)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaMySql(connectionString);
        return feature;
    }

    public static EFCoreWorkflowManagementPersistenceFeature UseMySql(this EFCoreWorkflowManagementPersistenceFeature feature, string connectionString)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaMySql(connectionString);
        return feature;
    }
}