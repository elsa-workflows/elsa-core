using Elsa.Persistence.EntityFrameworkCore.Common.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Elsa.ActivityDefinitions.EntityFrameworkCore.Sqlite
{
    public static class DbContextOptionsBuilderExtensions
    {
        public static DbContextOptionsBuilder UseSqlite(this DbContextOptionsBuilder builder, string connectionString = "Data Source=elsa.sqlite.db;Cache=Shared;") =>
            builder.UseSqlite(connectionString, db => db
                .MigrationsAssembly(typeof(SqliteDesignTimeActivityDefinitionsDbContextFactory).Assembly.GetName().Name)
                .MigrationsHistoryTable(ElsaDbContextBase.MigrationsHistoryTable, ElsaDbContextBase.ElsaSchema));
    }
}