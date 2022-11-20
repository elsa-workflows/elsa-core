using Elsa.Persistence.EntityFrameworkCore.Modules.ActivityDefinitions;
using Elsa.Persistence.EntityFrameworkCore.SqlServer.Extensions;

namespace Elsa.Persistence.EntityFrameworkCore.SqlServer.Modules.ActivityDefinitions;

public static class Extensions
{
    public static EFCoreActivityDefinitionsPersistenceFeature UseSqlServer(this EFCoreActivityDefinitionsPersistenceFeature feature, string connectionString)
    {
        feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlServer(connectionString);
        return feature;
    }
}