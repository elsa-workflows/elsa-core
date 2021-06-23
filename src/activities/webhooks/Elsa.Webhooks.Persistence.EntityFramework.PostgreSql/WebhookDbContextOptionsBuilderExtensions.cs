using Elsa.Webhooks.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Webhooks.Persistence.EntityFramework.PostgreSql
{
    public static class WebhookDbContextOptionsBuilderExtensions
    {
        /// <summary>
        /// Configures the context to use PostgreSql.
        /// </summary>
        public static DbContextOptionsBuilder UseWebhookPostgreSql(this DbContextOptionsBuilder builder, string connectionString) =>
            builder.UseNpgsql(connectionString, db => db
                .MigrationsAssembly(typeof(WebhookPostgreSqlElsaContextFactory).Assembly.GetName().Name)
                .MigrationsHistoryTable(WebhookContext.MigrationsHistoryTable, WebhookContext.ElsaSchema));
    }
}