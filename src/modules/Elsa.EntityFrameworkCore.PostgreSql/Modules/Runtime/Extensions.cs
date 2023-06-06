using Elsa.EntityFrameworkCore.Common;
using Elsa.EntityFrameworkCore.Modules.Runtime;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

public static partial class Extensions
{
    public static EFCoreDefaultWorkflowRuntimePersistenceFeature UsePostgreSql(this EFCoreDefaultWorkflowRuntimePersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaPostgreSql(connectionString, options);
        return feature;
    }
    
    public static EFCoreExecutionLogPersistenceFeature UsePostgreSql(this EFCoreExecutionLogPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaPostgreSql(connectionString, options);
        return feature;
    }
}