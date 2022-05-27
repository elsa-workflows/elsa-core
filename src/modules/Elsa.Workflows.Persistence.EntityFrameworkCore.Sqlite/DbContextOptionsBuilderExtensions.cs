using Elsa.Persistence.EntityFrameworkCore.Common.Abstractions;
using Elsa.Workflows.Persistence.EntityFrameworkCore.Features;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Workflows.Persistence.EntityFrameworkCore.Sqlite
{
    public static class DbContextOptionsBuilderExtensions
    {
        public static EFCoreWorkflowPersistenceFeature UseSqlite(this EFCoreWorkflowPersistenceFeature feature, string connectionString = "Data Source=elsa.sqlite.db;Cache=Shared;")
        {
            feature.ConfigureDbContextOptions((_, db) => db.UseSqlite(connectionString));
            return feature;
        }

        public static DbContextOptionsBuilder UseSqlite(this DbContextOptionsBuilder builder, string connectionString = "Data Source=elsa.sqlite.db;Cache=Shared;") =>
            builder.UseSqlite(connectionString, db => db
                .MigrationsAssembly(typeof(SqliteDesignTimeWorkflowsDbContextFactory).Assembly.GetName().Name)
                .MigrationsHistoryTable(ElsaDbContextBase.MigrationsHistoryTable, ElsaDbContextBase.ElsaSchema));
    }
}