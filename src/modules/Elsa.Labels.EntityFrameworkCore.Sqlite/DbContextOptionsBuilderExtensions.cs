using Elsa.Labels.EntityFrameworkCore.Options;
using Elsa.Persistence.EntityFrameworkCore.Common.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Labels.EntityFrameworkCore.Sqlite
{
    public static class DbContextOptionsBuilderExtensions
    {
        public static EFCoreLabelPersistenceConfigurator UseSqlite(this EFCoreLabelPersistenceConfigurator configurator, string connectionString = "Data Source=elsa.sqlite.db;Cache=Shared;")
        {
            configurator.ConfigureDbContextOptions((_, db) => db.UseSqlite(connectionString));
            return configurator;
        }

        public static DbContextOptionsBuilder UseSqlite(this DbContextOptionsBuilder builder, string connectionString = "Data Source=elsa.sqlite.db;Cache=Shared;") =>
            builder.UseSqlite(connectionString, db => db
                .MigrationsAssembly(typeof(SqliteDesignTimeLabelsDbContextFactory).Assembly.GetName().Name)
                .MigrationsHistoryTable(ElsaDbContextBase.MigrationsHistoryTable, ElsaDbContextBase.ElsaSchema));
    }
}