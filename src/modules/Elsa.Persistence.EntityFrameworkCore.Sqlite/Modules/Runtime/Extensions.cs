using Elsa.Persistence.EntityFrameworkCore.Modules.Runtime;

namespace Elsa.Persistence.EntityFrameworkCore.Sqlite.Modules.Runtime
{
    public static class Extensions
    {
        public static EFCoreRuntimePersistenceFeature UseSqlite(this EFCoreRuntimePersistenceFeature feature, string connectionString = Constants.DefaultConnectionString)
        {
            feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlite(connectionString);
            return feature;
        }
    }
}