using Elsa.Webhooks.Abstractions.Models;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Webhooks.Persistence.EntityFramework.Core
{
    public class WebhookContext : DbContext
    {
        public const string ElsaSchema = "Webhook";
        public const string MigrationsHistoryTable = "__EFWebhookMigrationsHistory";

        public WebhookContext(DbContextOptions options) : base(options)
        {
        }

        public virtual string Schema => ElsaSchema;
        public DbSet<WebhookDefinition> WebhookDefinitions { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}