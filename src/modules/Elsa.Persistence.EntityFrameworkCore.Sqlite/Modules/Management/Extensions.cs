using Elsa.Persistence.EntityFrameworkCore.Modules.Management;
using Elsa.Persistence.EntityFrameworkCore.Sqlite.Extensions;

namespace Elsa.Persistence.EntityFrameworkCore.Sqlite.Modules.Management
{
    public static class Extensions
    {
        public static EFCoreManagementPersistenceFeature UseSqlite(this EFCoreManagementPersistenceFeature feature, string connectionString = Constants.DefaultConnectionString)
        {
            feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlite(connectionString);
            return feature;
        }
    }
}