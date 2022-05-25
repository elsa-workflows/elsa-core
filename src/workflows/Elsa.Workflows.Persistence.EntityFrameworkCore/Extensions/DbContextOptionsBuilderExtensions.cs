using Elsa.Persistence.EntityFrameworkCore.Common.Abstractions;
using Elsa.Workflows.Persistence.EntityFrameworkCore.Options;
using Elsa.Workflows.Persistence.EntityFrameworkCore.Providers.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Workflows.Persistence.EntityFrameworkCore.Extensions
{
    public static class DbContextOptionsBuilderExtensions
    {
        public static EFCorePersistenceOptions UseSqlite(this EFCorePersistenceOptions builder, string connectionString = "Data Source=elsa.sqlite.db;Cache=Shared;")
        {
            builder.ConfigureDbContextOptions((_, x) => x.UseSqlite(connectionString, db => db
                .MigrationsAssembly(typeof(SqliteDesignTimeDbElsaContextFactory).Assembly.GetName().Name)
                .MigrationsHistoryTable(ElsaDbContextBase.MigrationsHistoryTable, ElsaDbContextBase.ElsaSchema)));

            return builder;
        }
    }
}