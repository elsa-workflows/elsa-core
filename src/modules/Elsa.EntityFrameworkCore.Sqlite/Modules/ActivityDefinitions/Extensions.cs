using Elsa.EntityFrameworkCore.Modules.ActivityDefinitions;
using Elsa.EntityFrameworkCore.Sqlite.Extensions;

namespace Elsa.EntityFrameworkCore.Sqlite.Modules.ActivityDefinitions;

public static class Extensions
{
    public static EFCoreActivityDefinitionsPersistenceFeature UseSqlite(this EFCoreActivityDefinitionsPersistenceFeature feature, string connectionString = Constants.DefaultConnectionString)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlite(connectionString);
        return feature;
    }
}