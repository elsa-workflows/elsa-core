using Elsa.Webhooks.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace Elsa.Webhooks.Persistence.EntityFramework.MySql
{
    public static class WebhookDbContextOptionsBuilderExtensions
    {
        /// <summary>
        /// Configures the context to use MySql 
        /// </summary>
        public static DbContextOptionsBuilder UseWebhookMySql(this DbContextOptionsBuilder builder, string connectionString) =>
            builder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), db => db
                .MigrationsAssembly(typeof(WebhookMySqlElsaContextFactory).Assembly.GetName().Name)
                .MigrationsHistoryTable(WebhookContext.MigrationsHistoryTable, WebhookContext.ElsaSchema)
                .SchemaBehavior(MySqlSchemaBehavior.Ignore));
    }
}