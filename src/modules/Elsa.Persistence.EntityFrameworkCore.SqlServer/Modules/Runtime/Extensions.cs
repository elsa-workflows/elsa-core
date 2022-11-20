using Elsa.Persistence.EntityFrameworkCore.Modules.Runtime;
using Elsa.Persistence.EntityFrameworkCore.SqlServer.Extensions;

namespace Elsa.Persistence.EntityFrameworkCore.SqlServer.Modules.Runtime;

public static class Extensions
{
    public static EFCoreRuntimePersistenceFeature UseSqlServer(this EFCoreRuntimePersistenceFeature feature, string connectionString)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlServer(connectionString);
        return feature;
    }
}