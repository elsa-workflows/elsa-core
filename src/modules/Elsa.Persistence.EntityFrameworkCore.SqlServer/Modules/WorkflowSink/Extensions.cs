using Elsa.Persistence.EntityFrameworkCore.Modules.WorkflowSink;
using Elsa.Persistence.EntityFrameworkCore.SqlServer.Extensions;

namespace Elsa.Persistence.EntityFrameworkCore.SqlServer.Modules.WorkflowSink;

public static class Extensions
{
    public static EFCoreWorkflowSinkPersistenceFeature UseSqlServer(this EFCoreWorkflowSinkPersistenceFeature feature, string connectionString)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlServer(connectionString);
        return feature;
    }
}