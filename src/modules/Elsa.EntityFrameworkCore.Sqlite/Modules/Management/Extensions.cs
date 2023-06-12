using Elsa.EntityFrameworkCore.Common;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Sqlite;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

[PublicAPI]
public static partial class Extensions
{
    public static EFCoreWorkflowDefinitionPersistenceFeature UseSqlite(this EFCoreWorkflowDefinitionPersistenceFeature feature, string connectionString = Constants.DefaultConnectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlite(connectionString, options);
        return feature;
    }
    
    public static EFCoreWorkflowInstancePersistenceFeature UseSqlite(this EFCoreWorkflowInstancePersistenceFeature feature, string connectionString = Constants.DefaultConnectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlite(connectionString, options);
        return feature;
    }
    
    public static EFCoreWorkflowManagementPersistenceFeature UseSqlite(this EFCoreWorkflowManagementPersistenceFeature feature, string connectionString = Constants.DefaultConnectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlite(connectionString, options);
        return feature;
    }
}