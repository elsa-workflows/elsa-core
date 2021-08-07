using Elsa.Webhooks.Persistence.EntityFramework.Core;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Webhooks.Persistence.EntityFramework.Sqlite
{
    public static class WebhookDbContextOptionsBuilderExtensions
    {
        public static DbContextOptionsBuilder UseWebhookSqlite(this DbContextOptionsBuilder builder, string connectionString = "Data Source=elsa.sqlite.db;Cache=Shared;") => builder.UseSqlite(connectionString, db => db
            .MigrationsAssembly(typeof(WebhookSqliteContextFactory).Assembly.GetName().Name)
            .MigrationsHistoryTable(WebhookContext.MigrationsHistoryTable, WebhookContext.ElsaSchema));
    }
}