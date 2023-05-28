using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.EntityFrameworkCore.Common;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

public static partial class Extensions
{
    public static EFCoreDefaultWorkflowRuntimePersistenceFeature UseMySql(this EFCoreDefaultWorkflowRuntimePersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaMySql(connectionString, options);
        return feature;
    }

    public static EFCoreExecutionLogRecordPersistenceFeature UseMySql(this EFCoreExecutionLogRecordPersistenceFeature feature, string connectionString, ElsaDbContextOptions? options = default)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaMySql(connectionString, options);
        return feature;
    }
}