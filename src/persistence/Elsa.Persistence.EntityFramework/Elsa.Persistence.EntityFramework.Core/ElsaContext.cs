using System.Linq;
using Elsa.Models;
using Elsa.Persistence.EntityFramework.Core.Configuration;
using Elsa.Persistence.EntityFramework.Core.Extensions;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Elsa.Persistence.EntityFramework.Core
{
    public class ElsaContext : DbContext
    {
        public const string ElsaSchema = "Elsa";
        public const string MigrationsHistoryTable = "__EFMigrationsHistory";

        public ElsaContext(DbContextOptions options) : base(options)
        {
        }

        public virtual string Schema => ElsaSchema;
        public DbSet<WorkflowDefinition> WorkflowDefinitions { get; set; } = default!;
        public DbSet<WorkflowInstance> WorkflowInstances { get; set; } = default!;
        public DbSet<WorkflowExecutionLogRecord> WorkflowExecutionLogRecords { get; set; } = default!;
        public DbSet<Bookmark> Bookmarks { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            if (!string.IsNullOrWhiteSpace(Schema))
                modelBuilder.HasDefaultSchema(Schema);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ElsaContext).Assembly);

            if (Database.IsSqlite())
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

            if (Database.IsOracle())
            {
                // In order to use data more than 2000 char we have to use NCLOB. In oracle we have to explicitly say the column is NCLOB otherwise it would be considered nvarchar(2000).
                modelBuilder.Entity<WorkflowInstance>().Property(x => x.LastExecutedActivityId).HasColumnType("NCLOB");
                modelBuilder.Entity<WorkflowInstance>().Property("Data").HasColumnType("NCLOB");
                modelBuilder.Entity<WorkflowExecutionLogRecord>().Property(x => x.Source).HasColumnType("NCLOB");
                modelBuilder.Entity<WorkflowExecutionLogRecord>().Property(x => x.Message).HasColumnType("NCLOB");
                modelBuilder.Entity<WorkflowExecutionLogRecord>().Property(x => x.EventName).HasColumnType("NCLOB");
                modelBuilder.Entity<WorkflowExecutionLogRecord>().Property(x => x.Data).HasColumnType("NCLOB");
                modelBuilder.Entity<WorkflowDefinition>().Property(x => x.DisplayName).HasColumnType("NCLOB");
                modelBuilder.Entity<WorkflowDefinition>().Property(x => x.Description).HasColumnType("NCLOB");
                modelBuilder.Entity<WorkflowDefinition>().Property("Data").HasColumnType("NCLOB");
                modelBuilder.Entity<Bookmark>().Property(x => x.Model).HasColumnType("NCLOB");
                modelBuilder.Entity<Bookmark>().Property(x => x.ModelType).HasColumnType("NCLOB");
            }
        }
    }
}