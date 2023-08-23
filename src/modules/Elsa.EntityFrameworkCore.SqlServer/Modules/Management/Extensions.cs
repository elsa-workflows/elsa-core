using Elsa.EntityFrameworkCore.Common;
using Elsa.EntityFrameworkCore.Modules.Management;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

[PublicAPI]
public static partial class Extensions
{
    public static EFCoreWorkflowDefinitionPersistenceFeature UseSqlServer(this EFCoreWorkflowDefinitionPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlServer(connectionString, options);
        return feature;
    }
    
    public static EFCoreWorkflowInstancePersistenceFeature UseSqlServer(this EFCoreWorkflowInstancePersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlServer(connectionString, options);
        return feature;
    }
    
    public static WorkflowManagementPersistenceFeature UseSqlServer(this WorkflowManagementPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlServer(connectionString, options);
        return feature;
    }
}