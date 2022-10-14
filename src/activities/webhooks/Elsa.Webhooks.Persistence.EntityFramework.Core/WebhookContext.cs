using System.Linq;
using Elsa.Persistence.EntityFramework.Core.Configuration;
using Elsa.Webhooks.Models;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Elsa.Webhooks.Persistence.EntityFramework.Core
{
    public class WebhookContext : DbContext
    {
        public const string ElsaSchema = "Elsa";
        public const string MigrationsHistoryTable = "__EFMigrationsHistory";

        public WebhookContext(DbContextOptions options) : base(options)
        {
        }

        public virtual string Schema => ElsaSchema;
        public DbSet<WebhookDefinition> WebhookDefinitions { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            if (!string.IsNullOrWhiteSpace(Schema))
                modelBuilder.HasDefaultSchema(Schema);
            
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(WebhookContext).Assembly);

#if NET7_0_OR_GREATER
            if (Database.ProviderName is "Microsoft.EntityFrameworkCore.Sqlite")
#else
            if (Database.IsSqlite())
#endif
            {
                // SQLite does not have proper support for DateTimeOffset via Entity Framework Core, see the limitations
                // here: https://docs.microsoft.com/en-us/ef/core/providers/sqlite/limitations#query-limitations
                foreach (var entityType in modelBuilder.Model.GetEntityTypes())
                {
                    var properties = entityType.ClrType.GetProperties().Where(p => p.PropertyType == typeof(Instant) || p.PropertyType == typeof(Instant?));
                    foreach (var property in properties)
                    {
                        modelBuilder
                            .Entity(entityType.Name)
                            .Property(property.Name)
                            .HasConversion(ValueConverters.SqliteInstantConverter);
                    }
                }
            }
        }
    }
}