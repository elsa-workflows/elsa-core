using System;
using System.Linq;
using Elsa.Webhooks.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Elsa.Webhooks.Persistence.EntityFramework.MySql
{
    public class WebhookMySqlElsaContextFactory : IDesignTimeDbContextFactory<WebhookContext>
    {
        public WebhookContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<WebhookContext>();
            var connectionString = args.Any() ? args[0] : throw new InvalidOperationException("Please specify a connection string. E.g. dotnet ef database update -- \"Server=localhost;Port=3306;Database=elsa;User=root;Password=password\"");
            var serverVersion = args.Length >= 2 ? args[1] : null;

            builder.UseMySql(
                connectionString,
                serverVersion != null ? ServerVersion.Parse(serverVersion) : ServerVersion.AutoDetect(connectionString),
                db => db
                    .MigrationsAssembly(typeof(WebhookMySqlElsaContextFactory).Assembly.GetName().Name)
                    .MigrationsHistoryTable(WebhookContext.MigrationsHistoryTable, WebhookContext.ElsaSchema));

            return new WebhookContext(builder.Options);
        }
    }
}