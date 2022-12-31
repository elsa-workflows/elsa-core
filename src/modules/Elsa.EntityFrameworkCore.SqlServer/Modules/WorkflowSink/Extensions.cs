using Elsa.EntityFrameworkCore.Modules.WorkflowSink;
using Elsa.EntityFrameworkCore.SqlServer.Extensions;

namespace Elsa.EntityFrameworkCore.SqlServer.Modules.WorkflowSink;

public static class Extensions
{
    public static EFCoreWorkflowSinkPersistenceFeature UseSqlServer(this EFCoreWorkflowSinkPersistenceFeature feature, string connectionString)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlServer(connectionString);
        return feature;
    }
}