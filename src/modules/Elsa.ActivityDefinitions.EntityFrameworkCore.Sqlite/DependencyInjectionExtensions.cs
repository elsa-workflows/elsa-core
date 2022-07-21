using Elsa.ActivityDefinitions.EntityFrameworkCore.Features;

namespace Elsa.ActivityDefinitions.EntityFrameworkCore.Sqlite
{
    public static class DependencyInjectionExtensions
    {
        public static EFCoreActivityDefinitionsPersistenceFeature UseSqlite(this EFCoreActivityDefinitionsPersistenceFeature feature, string connectionString = "Data Source=elsa.sqlite.db;Cache=Shared;")
        {
            feature.ConfigureDbContextOptions((_, db) => db.UseSqlite(connectionString));
            return feature;
        }
    }
}