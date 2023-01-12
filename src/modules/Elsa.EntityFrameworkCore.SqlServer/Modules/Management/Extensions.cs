using Elsa.EntityFrameworkCore.Modules.Management;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

[PublicAPI]
public static partial class Extensions
{
    public static EFCoreWorkflowDefinitionPersistenceFeature UseSqlServer(this EFCoreWorkflowDefinitionPersistenceFeature feature, string connectionString)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlServer(connectionString);
        return feature;
    }
    
    public static EFCoreWorkflowInstancePersistenceFeature UseSqlServer(this EFCoreWorkflowInstancePersistenceFeature feature, string connectionString)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlServer(connectionString);
        return feature;
    }
    
    public static EFCoreWorkflowManagementPersistenceFeature UseSqlServer(this EFCoreWorkflowManagementPersistenceFeature feature, string connectionString)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlServer(connectionString);
        return feature;
    }
}