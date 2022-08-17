using System.Linq;
using Elsa.Secrets.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Elsa.Secrets.Persistence.EntityFramework.Sqlite
{
    public class SecretsSqliteContextFactory : IDesignTimeDbContextFactory<SecretsContext>
    {
        public SecretsContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<SecretsContext>();
            var connectionString = args.Any() ? args[0] : "Data Source=elsa.db;Cache=Shared";

            builder.UseSqlite(connectionString, db => db
                .MigrationsAssembly(typeof(SecretsSqliteContextFactory).Assembly.GetName().Name)
                .MigrationsHistoryTable(SecretsContext.MigrationsHistoryTable, SecretsContext.ElsaSchema));

            return new SecretsContext(builder.Options);
        }
    }
}