using Elsa.EntityFrameworkCore.Modules.Runtime;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

public static partial class Extensions
{
    public static EFCoreDefaultRuntimePersistenceFeature UseSqlServer(this EFCoreDefaultRuntimePersistenceFeature feature, string connectionString)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlServer(connectionString);
        return feature;
    }
}