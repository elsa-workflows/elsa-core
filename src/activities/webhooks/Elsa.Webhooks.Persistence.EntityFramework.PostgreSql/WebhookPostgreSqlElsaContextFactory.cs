using System.Linq;
using Elsa.Webhooks.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Elsa.Webhooks.Persistence.EntityFramework.PostgreSql
{
    public class WebhookPostgreSqlElsaContextFactory : IDesignTimeDbContextFactory<WebhookContext>
    {
        public WebhookContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<WebhookContext>();
            var connectionString = args.Any() ? args[0] : "Server=127.0.0.1;Port=5432;Database=elsa;User Id=postgres;Password=password;";

            builder.UseNpgsql(
                connectionString,
                db => db.MigrationsAssembly(typeof(WebhookPostgreSqlElsaContextFactory).Assembly.GetName().Name)
                    .MigrationsHistoryTable(WebhookContext.MigrationsHistoryTable, WebhookContext.ElsaSchema));

            return new WebhookContext(builder.Options);
        }
    }
}