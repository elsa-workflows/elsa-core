using Elsa.EntityFrameworkCore.Modules.Management;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

public static partial class Extensions
{
    public static EFCoreDefaultManagementPersistenceFeature UseSqlServer(this EFCoreDefaultManagementPersistenceFeature feature, string connectionString)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlServer(connectionString);
        return feature;
    }
}