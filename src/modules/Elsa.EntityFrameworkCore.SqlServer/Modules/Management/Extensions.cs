using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.SqlServer.Extensions;

namespace Elsa.EntityFrameworkCore.SqlServer.Modules.Management;

public static class Extensions
{
    public static EFCoreManagementPersistenceFeature UseSqlServer(this EFCoreManagementPersistenceFeature feature, string connectionString)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlServer(connectionString);
        return feature;
    }
}