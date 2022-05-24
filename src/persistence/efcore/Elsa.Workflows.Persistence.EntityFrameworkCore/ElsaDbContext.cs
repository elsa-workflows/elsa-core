using Elsa.Persistence.Entities;
using Elsa.Workflows.Persistence.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Elsa.Workflows.Persistence.EntityFrameworkCore;

public class ElsaDbContext : DbContext
{
    public const string ElsaSchema = "Elsa";
    public const string MigrationsHistoryTable = "__EFMigrationsHistory";

    public ElsaDbContext(DbContextOptions options) : base(options)
    {
    }

    protected virtual string Schema => ElsaSchema;
    public DbSet<WorkflowDefinition> WorkflowDefinitions { get; set; } = default!;
    public DbSet<WorkflowInstance> WorkflowInstances { get; set; } = default!;
    public DbSet<WorkflowTrigger> WorkflowTriggers { get; set; } = default!;
    public DbSet<WorkflowBookmark> WorkflowBookmarks { get; set; } = default!;
    public DbSet<WorkflowExecutionLogRecord> WorkflowExecutionLogRecords { get; set; } = default!;
    public DbSet<Label> Labels { get; set; } = default!;
    public DbSet<WorkflowDefinitionLabel> WorkflowDefinitionLabels { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        if (!string.IsNullOrWhiteSpace(Schema))
        {
            if (!Database.IsSqlite())
                modelBuilder.HasDefaultSchema(Schema);
        }

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ElsaDbContext).Assembly);

        SetupForSqlite(modelBuilder);
        SetupForOracle(modelBuilder);
    }

    private void SetupForSqlite(ModelBuilder modelBuilder)
    {
        if (!Database.IsSqlite()) return;
        
        // SQLite does not have proper support for DateTimeOffset via Entity Framework Core, see the limitations
        // here: https://docs.microsoft.com/en-us/ef/core/providers/sqlite/limitations#query-limitations
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var properties = entityType.ClrType.GetProperties().Where(p => p.PropertyType == typeof(DateTimeOffset) || p.PropertyType == typeof(DateTimeOffset?));

            foreach (var property in properties)
            {
                modelBuilder
                    .Entity(entityType.Name)
                    .Property(property.Name)
                    .HasConversion(new DateTimeOffsetToStringConverter());
            }
        }
    }

    private void SetupForOracle(ModelBuilder modelBuilder)
    {
        if (!Database.IsOracle()) return;
        
        // In order to use data more than 2000 char we have to use NCLOB. In oracle we have to explicitly say the column is NCLOB otherwise it would be considered nvarchar(2000).
        modelBuilder.Entity<WorkflowInstance>().Property("Data").HasColumnType("NCLOB");
        modelBuilder.Entity<WorkflowDefinition>().Property("Data").HasColumnType("NCLOB");
        modelBuilder.Entity<WorkflowBookmark>().Property("Data").HasColumnType("NCLOB");
    }
}