using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Elsa.Persistence.EntityFrameworkCore.Sqlite
{
    public class SqliteElsaContextFactory : IDesignTimeDbContextFactory<ElsaDbContext>
    {
        public ElsaDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<ElsaDbContext>();
            var connectionString = args.Any() ? args[0] : "Data Source=elsa.sqlite.db;Cache=Shared";

            builder.UseSqlite(connectionString, db => db
                .MigrationsAssembly(typeof(SqliteElsaContextFactory).Assembly.GetName().Name)
                .MigrationsHistoryTable(ElsaDbContext.MigrationsHistoryTable, ElsaDbContext.ElsaSchema));

            return new ElsaDbContext(builder.Options);
        }
    }
}