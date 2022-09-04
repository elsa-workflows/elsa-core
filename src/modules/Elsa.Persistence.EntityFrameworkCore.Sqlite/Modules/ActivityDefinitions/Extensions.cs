using Elsa.Persistence.EntityFrameworkCore.Modules.ActivityDefinitions;
using Elsa.Persistence.EntityFrameworkCore.Sqlite.Extensions;

namespace Elsa.Persistence.EntityFrameworkCore.Sqlite.Modules.ActivityDefinitions
{
    public static class Extensions
    {
        public static EFCoreActivityDefinitionsPersistenceFeature UseSqlite(this EFCoreActivityDefinitionsPersistenceFeature feature, string connectionString = Constants.DefaultConnectionString)
        {
            feature.DbContextOptionsBuilder = (_, db) => db.UseElsaSqlite(connectionString);
            return feature;
        }
    }
}