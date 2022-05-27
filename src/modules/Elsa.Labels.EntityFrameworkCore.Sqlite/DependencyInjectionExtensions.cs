using Elsa.Labels.EntityFrameworkCore.Configuration;
using Elsa.Labels.EntityFrameworkCore.Features;

namespace Elsa.Labels.EntityFrameworkCore.Sqlite
{
    public static class DependencyInjectionExtensions
    {
        public static EFCoreLabelPersistenceFeature UseSqlite(this EFCoreLabelPersistenceFeature feature, string connectionString = "Data Source=elsa.sqlite.db;Cache=Shared;")
        {
            feature.ConfigureDbContextOptions((_, db) => db.UseSqlite(connectionString));
            return feature;
        }
    }
}