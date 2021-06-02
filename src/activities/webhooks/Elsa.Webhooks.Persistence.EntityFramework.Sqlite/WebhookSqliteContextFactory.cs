using System.Linq;
using Elsa.Webhooks.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Elsa.Webhooks.Persistence.EntityFramework.Sqlite
{
    public class WebhookSqliteContextFactory : IDesignTimeDbContextFactory<WebhookContext>
    {
        public WebhookContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<WebhookContext>();
            var connectionString = args.Any() ? args[0] : "Data Source=elsa.db;Cache=Shared";

            builder.UseSqlite(connectionString, db => db
                .MigrationsAssembly(typeof(WebhookSqliteContextFactory).Assembly.GetName().Name)
                .MigrationsHistoryTable(WebhookContext.MigrationsHistoryTable, WebhookContext.ElsaSchema));

            return new WebhookContext(builder.Options);
        }
    }
}