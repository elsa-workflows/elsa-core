using System.Linq;
using Elsa.Persistence.EntityFramework.Core.Configuration;
using Elsa.Secrets.Models;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Elsa.Secrets.Persistence.EntityFramework.Core
{
    public class SecretsContext : DbContext
    {
        public const string ElsaSchema = "Elsa";
        public const string MigrationsHistoryTable = "__EFMigrationsHistory";

        public SecretsContext(DbContextOptions options) : base(options)
        {
        }

        public virtual string Schema => ElsaSchema;
        public DbSet<Secret> Secrets { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            if (!string.IsNullOrWhiteSpace(Schema))
                modelBuilder.HasDefaultSchema(Schema);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(SecretsContext).Assembly);

#if NET7_0_OR_GREATER
            if (Database.ProviderName is "Microsoft.EntityFrameworkCore.Sqlite")
#else
            if (Database.IsSqlite())
#endif
            {
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