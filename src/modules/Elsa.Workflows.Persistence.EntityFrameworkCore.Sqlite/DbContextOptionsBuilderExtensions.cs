using Elsa.Persistence.EntityFrameworkCore.Common.Abstractions;
using Elsa.Workflows.Persistence.EntityFrameworkCore.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Workflows.Persistence.EntityFrameworkCore.Sqlite
{
    public static class DbContextOptionsBuilderExtensions
    {
        public static EFCoreWorkflowPersistenceConfigurator UseSqlite(this EFCoreWorkflowPersistenceConfigurator configurator, string connectionString = "Data Source=elsa.sqlite.db;Cache=Shared;")
        {
            configurator.ConfigureDbContextOptions((_, db) => db.UseSqlite(connectionString));
            return configurator;
        }

        public static DbContextOptionsBuilder UseSqlite(this DbContextOptionsBuilder builder, string connectionString = "Data Source=elsa.sqlite.db;Cache=Shared;") =>
            builder.UseSqlite(connectionString, db => db
                .MigrationsAssembly(typeof(SqliteDesignTimeWorkflowsDbContextFactory).Assembly.GetName().Name)
                .MigrationsHistoryTable(ElsaDbContextBase.MigrationsHistoryTable, ElsaDbContextBase.ElsaSchema));
    }
}