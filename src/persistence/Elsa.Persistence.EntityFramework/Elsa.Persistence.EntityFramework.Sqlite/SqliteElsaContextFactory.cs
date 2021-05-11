using System.Linq;
using Elsa.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Elsa.Persistence.EntityFramework.Sqlite
{
    public class SqliteElsaContextFactory : IDesignTimeDbContextFactory<ElsaContext>
    {
        public ElsaContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<ElsaContext>();
            var connectionString = args.Any() ? args[0] : "Data Source=elsa.db;Cache=Shared";
            
            builder.UseSqlite(connectionString, db => db
                .MigrationsAssembly(typeof(SqliteElsaContextFactory).Assembly.GetName().Name)
                .MigrationsHistoryTable(ElsaContext.MigrationsHistoryTable, ElsaContext.ElsaSchema));
            
            return new ElsaContext(builder.Options);
        }
    }
}