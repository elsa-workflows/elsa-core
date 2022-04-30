using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EntityFrameworkCore.Sqlite
{
    public static class DbContextOptionsBuilderExtensions
    {
        public static DbContextOptionsBuilder UseSqlite(this DbContextOptionsBuilder builder, string connectionString = "Data Source=elsa.sqlite.db;Cache=Shared;") => builder.UseSqlite(connectionString, db => db
            .MigrationsAssembly(typeof(SqliteElsaContextFactory).Assembly.GetName().Name)
            .MigrationsHistoryTable(ElsaDbContext.MigrationsHistoryTable, ElsaDbContext.ElsaSchema));
    }
}