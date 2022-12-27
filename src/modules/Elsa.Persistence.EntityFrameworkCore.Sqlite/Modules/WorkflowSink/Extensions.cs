using Elsa.Persistence.EntityFrameworkCore.Modules.WorkflowSink;
using Elsa.Persistence.EntityFrameworkCore.Sqlite.Extensions;

namespace Elsa.Persistence.EntityFrameworkCore.Sqlite.Modules.WorkflowSink;

public static class Extensions
{
    public static EFCoreWorkflowSinkPersistenceFeature UseSqlite(this EFCoreWorkflowSinkPersistenceFeature feature, string connectionString = Constants.DefaultConnectionString)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlite(connectionString);
        return feature;
    }
}