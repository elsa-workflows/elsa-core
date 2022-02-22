using Elsa.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.Linq;

namespace Elsa.Persistence.EntityFramework.Oracle
{
    public class OracleElsaContextFactory: IDesignTimeDbContextFactory<ElsaContext>
    {
        public ElsaContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<ElsaContext>();
            var connectionString = args.Any() ? args[0] : "Server=127.0.0.1;Port=5432;Database=elsa;User Id=oracle;Password=password;";

            builder.UseOracle(
                connectionString,
                db => db.MigrationsAssembly(typeof(OracleElsaContextFactory).Assembly.GetName().Name)
                    .MigrationsHistoryTable(ElsaContext.MigrationsHistoryTable, ElsaContext.ElsaSchema));

            return new ElsaContext(builder.Options);
        }
    }
}
