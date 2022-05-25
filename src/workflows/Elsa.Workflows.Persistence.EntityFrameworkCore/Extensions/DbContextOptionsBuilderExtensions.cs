using Elsa.Persistence.EntityFrameworkCore.Common;
using Elsa.Workflows.Persistence.EntityFrameworkCore.DbContextFactories;
using Elsa.Workflows.Persistence.EntityFrameworkCore.DbContexts;
using Elsa.Workflows.Persistence.EntityFrameworkCore.Options;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Workflows.Persistence.EntityFrameworkCore.Extensions
{
    public static class DbContextOptionsBuilderExtensions
    {
        public static EFCorePersistenceOptions UseSqlite(this EFCorePersistenceOptions builder, string connectionString = "Data Source=elsa.sqlite.db;Cache=Shared;")
        {
            builder.ConfigureDbContextOptions((_, x) => x.UseSqlite(connectionString, db => db
                .MigrationsAssembly(typeof(SqliteElsaContextFactory).Assembly.GetName().Name)
                .MigrationsHistoryTable(ElsaDbContextBase.MigrationsHistoryTable, ElsaDbContext.ElsaSchema)));

            return builder;
        }
    }
}