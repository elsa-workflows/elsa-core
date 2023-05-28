using Elsa.EntityFrameworkCore.Common;
using Elsa.EntityFrameworkCore.Modules.Runtime;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

public static partial class Extensions
{
    public static EFCoreDefaultWorkflowRuntimePersistenceFeature UseSqlServer(this EFCoreDefaultWorkflowRuntimePersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlServer(connectionString, options);
        return feature;
    }

    public static EFCoreExecutionLogRecordPersistenceFeature UseSqlServer(this EFCoreExecutionLogRecordPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlServer(connectionString, options);
        return feature;
    }
}