using Elsa.Labels.EntityFrameworkCore.Options;

namespace Elsa.Labels.EntityFrameworkCore.Sqlite
{
    public static class DependencyInjectionExtensions
    {
        public static EFCoreLabelPersistenceConfigurator UseSqlite(this EFCoreLabelPersistenceConfigurator configurator, string connectionString = "Data Source=elsa.sqlite.db;Cache=Shared;")
        {
            configurator.ConfigureDbContextOptions((_, db) => db.UseSqlite(connectionString));
            return configurator;
        }
    }
}