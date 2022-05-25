using Elsa.Persistence.EntityFrameworkCore.Common;
using Elsa.Workflows.Persistence.EntityFrameworkCore.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Elsa.Workflows.Persistence.EntityFrameworkCore.DbContextFactories
{
    public class SqliteElsaContextFactory : IDesignTimeDbContextFactory<SqliteElsaDbContext>
    {
        public SqliteElsaDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<ElsaDbContext>();
            var connectionString = args.Any() ? args[0] : "Data Source=elsa.sqlite.db;Cache=Shared";

            builder.UseSqlite(connectionString, db => db
                .MigrationsAssembly(typeof(SqliteElsaContextFactory).Assembly.GetName().Name)
                .MigrationsHistoryTable(ElsaDbContextBase.MigrationsHistoryTable, ElsaDbContextBase.ElsaSchema));

            return new SqliteElsaDbContext(builder.Options);
        }
    }
}