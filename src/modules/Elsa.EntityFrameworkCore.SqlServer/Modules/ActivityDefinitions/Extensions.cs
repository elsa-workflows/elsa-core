using Elsa.EntityFrameworkCore.Modules.ActivityDefinitions;
using Elsa.EntityFrameworkCore.SqlServer.Extensions;

namespace Elsa.EntityFrameworkCore.SqlServer.Modules.ActivityDefinitions;

public static class Extensions
{
    public static EFCoreActivityDefinitionsPersistenceFeature UseSqlServer(this EFCoreActivityDefinitionsPersistenceFeature feature, string connectionString)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlServer(connectionString);
        return feature;
    }
}