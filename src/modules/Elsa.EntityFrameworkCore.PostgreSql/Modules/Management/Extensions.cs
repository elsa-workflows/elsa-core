using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

public static partial class Extensions
{
    public static EFCoreManagementPersistenceFeature UsePostgreSql(this EFCoreManagementPersistenceFeature feature, string connectionString)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaPostgreSql(connectionString);
        return feature;
    }
    
    public static EFCoreWorkflowInstancePersistenceFeature UsePostgreSql(this EFCoreWorkflowInstancePersistenceFeature feature, string connectionString)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaPostgreSql(connectionString);
        return feature;
    }
}