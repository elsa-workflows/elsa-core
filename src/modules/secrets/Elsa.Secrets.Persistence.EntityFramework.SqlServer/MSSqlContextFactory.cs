using System;
using System.Linq;
using Elsa.Secrets.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Elsa.Secrets.Persistence.EntityFramework.SqlServer
{
    public class MSSqlContextFactory : IDesignTimeDbContextFactory<SecretsContext>
    {
        public SecretsContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<SecretsContext>();
            var connectionString = args.Any() ? args[0] : throw new InvalidOperationException("Please specify a connection string. E.g. dotnet ef database update -- \"Server=localhost;Port=3306;Database=elsa;User=root;Password=password\"");
            var serverVersion = args.Length >= 2 ? args[1] : null;

            builder.UseSqlServer(
                connectionString,
                db => db
                    .MigrationsAssembly(typeof(MSSqlContextFactory).Assembly.GetName().Name)
                    .MigrationsHistoryTable(SecretsContext.MigrationsHistoryTable, SecretsContext.ElsaSchema));

            return new SecretsContext(builder.Options);
        }
    }
}