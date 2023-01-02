using Elsa.EntityFrameworkCore.Modules.Management;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

public static partial class Extensions
{
    public static EFCoreManagementPersistenceFeature UsePostgreSql(this EFCoreManagementPersistenceFeature feature, string connectionString)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaPostgreSql(connectionString);
        return feature;
    }
}