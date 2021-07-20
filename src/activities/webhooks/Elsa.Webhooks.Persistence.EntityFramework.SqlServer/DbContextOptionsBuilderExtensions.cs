using Elsa.Webhooks.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Webhooks.Persistence.EntityFramework.SqlServer
{
    public static class DbContextOptionsBuilderExtensions
    {
        /// <summary>
        /// Configures the context to use SqlServer.
        /// </summary>
        public static DbContextOptionsBuilder UseWebhookSqlServer(this DbContextOptionsBuilder builder, string connectionString) =>
            builder.UseSqlServer(connectionString, db => db
                .MigrationsAssembly(typeof(WebhookSqlServerElsaContextFactory).Assembly.GetName().Name)
                .MigrationsHistoryTable(WebhookContext.MigrationsHistoryTable, WebhookContext.ElsaSchema));
    }
}