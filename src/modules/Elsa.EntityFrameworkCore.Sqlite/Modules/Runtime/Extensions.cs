using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.EntityFrameworkCore.Sqlite.Extensions;

namespace Elsa.EntityFrameworkCore.Sqlite.Modules.Runtime;

public static class Extensions
{
    public static EFCoreRuntimePersistenceFeature UseSqlite(this EFCoreRuntimePersistenceFeature feature, string connectionString = Constants.DefaultConnectionString)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlite(connectionString);
        return feature;
    }
}