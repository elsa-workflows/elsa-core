using System.Linq;
using Elsa.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Elsa.Persistence.EntityFramework.PostgreSql
{
    public class PostgreSqlElsaContextFactory : IDesignTimeDbContextFactory<ElsaContext>
    {
        public ElsaContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<ElsaContext>();
            var connectionString = args.Any() ? args[0] : "Server=127.0.0.1;Port=5432;Database=elsa;User Id=postgres;Password=password;";
            
            builder.UseNpgsql(
                connectionString, 
                db => db.MigrationsAssembly(typeof(PostgreSqlElsaContextFactory).Assembly.GetName().Name)
                    .MigrationsHistoryTable(ElsaContext.MigrationsHistoryTable, ElsaContext.ElsaSchema));
            
            return new ElsaContext(builder.Options);
        }
    }
}