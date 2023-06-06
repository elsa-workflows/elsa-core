using Elsa.EntityFrameworkCore.Common;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.EntityFrameworkCore.Sqlite;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

public static partial class Extensions
{
    public static EFCoreWorkflowRuntimePersistenceFeature UseSqlite(this EFCoreWorkflowRuntimePersistenceFeature feature, string connectionString = Constants.DefaultConnectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlite(connectionString, options);
        return feature;
        
    }
    public static EFCoreDefaultWorkflowRuntimePersistenceFeature UseSqlite(this EFCoreDefaultWorkflowRuntimePersistenceFeature feature, string connectionString = Constants.DefaultConnectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlite(connectionString, options);
        return feature;
    }
    
    public static EFCoreExecutionLogPersistenceFeature UseSqlite(this EFCoreExecutionLogPersistenceFeature feature, string connectionString = Constants.DefaultConnectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlite(connectionString, options);
        return feature;
    }
}