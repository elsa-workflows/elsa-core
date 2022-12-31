using Elsa.EntityFrameworkCore.Modules.WorkflowSink;
using Elsa.EntityFrameworkCore.Sqlite.Extensions;

namespace Elsa.EntityFrameworkCore.Sqlite.Modules.WorkflowSink;

public static class Extensions
{
    public static EFCoreWorkflowSinkPersistenceFeature UseSqlite(this EFCoreWorkflowSinkPersistenceFeature feature, string connectionString = Constants.DefaultConnectionString)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlite(connectionString);
        return feature;
    }
}