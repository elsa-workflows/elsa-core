using System;
using System.Linq;
using Elsa.Webhooks.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Elsa.Webhooks.Persistence.EntityFramework.SqlServer
{
    public class WebhookSqlServerElsaContextFactory : IDesignTimeDbContextFactory<WebhookContext>
    {
        public WebhookContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<WebhookContext>();
            var connectionString = args.Any() ? args[0] : throw new InvalidOperationException("Please specify a connection string. E.g. dotnet ef database update -- \"Server=Local;Database=elsa\"");
            builder.UseSqlServer(connectionString, db => db
                .MigrationsAssembly(typeof(WebhookSqlServerElsaContextFactory).Assembly.GetName().Name)
                .MigrationsHistoryTable(WebhookContext.MigrationsHistoryTable, WebhookContext.ElsaSchema));
            return new WebhookContext(builder.Options);
        }
    }
}