using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.EntityFrameworkCore.Sqlite;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

public static partial class Extensions
{
    public static EFCoreWorkflowRuntimePersistenceFeature UseSqlite(this EFCoreWorkflowRuntimePersistenceFeature feature, string connectionString = Constants.DefaultConnectionString)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlite(connectionString);
        return feature;
        
    }
    public static EFCoreDefaultWorkflowRuntimePersistenceFeature UseSqlite(this EFCoreDefaultWorkflowRuntimePersistenceFeature feature, string connectionString = Constants.DefaultConnectionString)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlite(connectionString);
        return feature;
    }
    
    public static EFCoreExecutionLogRecordPersistenceFeature UseSqlite(this EFCoreExecutionLogRecordPersistenceFeature feature, string connectionString = Constants.DefaultConnectionString)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlite(connectionString);
        return feature;
    }
}