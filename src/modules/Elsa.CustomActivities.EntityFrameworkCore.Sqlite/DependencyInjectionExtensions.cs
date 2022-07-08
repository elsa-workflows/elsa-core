using Elsa.CustomActivities.EntityFrameworkCore.Features;

namespace Elsa.CustomActivities.EntityFrameworkCore.Sqlite
{
    public static class DependencyInjectionExtensions
    {
        public static EFCoreCustomActivitiesPersistenceFeature UseSqlite(this EFCoreCustomActivitiesPersistenceFeature feature, string connectionString = "Data Source=elsa.sqlite.db;Cache=Shared;")
        {
            feature.ConfigureDbContextOptions((_, db) => db.UseSqlite(connectionString));
            return feature;
        }
    }
}