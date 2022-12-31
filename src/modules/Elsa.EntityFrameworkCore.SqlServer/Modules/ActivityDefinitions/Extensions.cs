using Elsa.EntityFrameworkCore.Modules.ActivityDefinitions;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

public static partial class Extensions
{
    public static EFCoreActivityDefinitionsPersistenceFeature UseSqlServer(this EFCoreActivityDefinitionsPersistenceFeature feature, string connectionString)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlServer(connectionString);
        return feature;
    }
}