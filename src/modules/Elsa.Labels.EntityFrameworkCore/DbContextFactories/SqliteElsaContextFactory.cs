using Elsa.Labels.EntityFrameworkCore.DbContexts;
using Elsa.Persistence.EntityFrameworkCore.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Elsa.Labels.EntityFrameworkCore.DbContextFactories
{
    public class SqliteElsaContextFactory : IDesignTimeDbContextFactory<SqliteLabelsDbContext>
    {
        public SqliteLabelsDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<SqliteLabelsDbContext>();
            var connectionString = args.Any() ? args[0] : "Data Source=elsa.sqlite.db;Cache=Shared";

            builder.UseSqlite(connectionString, db => db
                .MigrationsAssembly(typeof(SqliteElsaContextFactory).Assembly.GetName().Name)
                .MigrationsHistoryTable(ElsaDbContextBase.MigrationsHistoryTable, ElsaDbContextBase.ElsaSchema));

            return new SqliteLabelsDbContext(builder.Options);
        }
    }
}