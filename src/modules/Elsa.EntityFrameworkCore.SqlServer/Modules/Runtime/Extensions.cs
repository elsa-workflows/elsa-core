using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.EntityFrameworkCore.SqlServer.Extensions;

namespace Elsa.EntityFrameworkCore.SqlServer.Modules.Runtime;

public static class Extensions
{
    public static EFCoreRuntimePersistenceFeature UseSqlServer(this EFCoreRuntimePersistenceFeature feature, string connectionString)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlServer(connectionString);
        return feature;
    }
}