using Elsa.EntityFrameworkCore.Modules.ActivityDefinitions;

// ReSharper disable once CheckNamespace
namespace Elsa.EntityFrameworkCore.Extensions;

public static partial class Extensions
{
    public static EFCoreActivityDefinitionsPersistenceFeature UsePostgreSql(this EFCoreActivityDefinitionsPersistenceFeature feature, string connectionString)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaPostgreSql(connectionString);
        return feature;
    }
}