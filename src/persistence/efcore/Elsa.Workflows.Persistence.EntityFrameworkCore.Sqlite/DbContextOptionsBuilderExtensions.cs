using Elsa.Workflows.Persistence.EntityFrameworkCore.Options;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Workflows.Persistence.EntityFrameworkCore.Sqlite
{
    public static class DbContextOptionsBuilderExtensions
    {
        public static EFCorePersistenceOptions UseSqlite(this EFCorePersistenceOptions builder, string connectionString = "Data Source=elsa.sqlite.db;Cache=Shared;")
        {
            builder.ConfigureDbContextOptions((_, x) => x.UseSqlite(connectionString, db => db
                .MigrationsAssembly(typeof(SqliteElsaContextFactory).Assembly.GetName().Name)
                .MigrationsHistoryTable(ElsaDbContext.MigrationsHistoryTable, ElsaDbContext.ElsaSchema)));

            return builder;
        }
    }
}