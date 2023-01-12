using Elsa.EntityFrameworkCore.Modules.Runtime;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

public static partial class Extensions
{
    public static EFCoreDefaultRuntimePersistenceFeature UsePostgreSql(this EFCoreDefaultRuntimePersistenceFeature feature, string connectionString)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaPostgreSql(connectionString);
        return feature;
    }
    
    public static EFCoreExecutionLogRecordPersistenceFeature UsePostgreSql(this EFCoreExecutionLogRecordPersistenceFeature feature, string connectionString)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaPostgreSql(connectionString);
        return feature;
    }
}