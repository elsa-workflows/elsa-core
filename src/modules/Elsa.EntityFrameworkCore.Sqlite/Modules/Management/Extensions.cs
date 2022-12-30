using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Sqlite.Extensions;

namespace Elsa.EntityFrameworkCore.Sqlite.Modules.Management;

public static class Extensions
{
    public static EFCoreManagementPersistenceFeature UseSqlite(this EFCoreManagementPersistenceFeature feature, string connectionString = Constants.DefaultConnectionString)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlite(connectionString);
        return feature;
    }
}