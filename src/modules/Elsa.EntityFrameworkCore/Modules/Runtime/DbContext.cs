using Elsa.EntityFrameworkCore.Common;
using Elsa.KeyValues.Entities;
using Elsa.Workflows.Runtime.Entities;
using Microsoft.EntityFrameworkCore;

namespace Elsa.EntityFrameworkCore.Modules.Runtime;

/// <summary>
/// DB context for the runtime module.
/// </summary>
public class RuntimeElsaDbContext : ElsaDbContextBase
{
    /// <inheritdoc />
    public RuntimeElsaDbContext(DbContextOptions options) : base(options)
    {
    }
    
    /// <summary>
    /// The workflow triggers.
    /// </summary>
    public DbSet<StoredTrigger> Triggers { get; set; } = default!;
    
    /// <summary>
    /// The workflow execution log records.
    /// </summary>
    public DbSet<WorkflowExecutionLogRecord> WorkflowExecutionLogRecords { get; set; } = default!;

    /// <summary>
    /// The activity execution records.
    /// </summary>
    public DbSet<ActivityExecutionRecord> ActivityExecutionRecords { get; set; } = default!;
    
    /// <summary>
    /// The workflow bookmarks.
    /// </summary>
    public DbSet<StoredBookmark> Bookmarks { get; set; } = default!;
    
    /// <summary>
    /// The generic key value pairs.
    /// </summary>
    public DbSet<SerializedKeyValuePair> KeyValuePairs { get; set; } = default!;

    /// <inheritdoc />
    protected override void ApplyEntityConfigurations(ModelBuilder modelBuilder)
    {
        var config = new Configurations();
        modelBuilder.ApplyConfiguration<StoredTrigger>(config);
        modelBuilder.ApplyConfiguration<WorkflowExecutionLogRecord>(config);
        modelBuilder.ApplyConfiguration<ActivityExecutionRecord>(config);
        modelBuilder.ApplyConfiguration<StoredBookmark>(config);
        modelBuilder.ApplyConfiguration<SerializedKeyValuePair>(config);
    }

        /// <inheritdoc />
    protected override void SetupForOracle(ModelBuilder modelBuilder)
    {
        // In order to use data more than 2000 char we have to use NCLOB.
        // In oracle we have to explicitly say the column is NCLOB otherwise it would be considered nvarchar(2000).

        modelBuilder.Entity<StoredTrigger>().Property("SerializedPayload").HasColumnType("NCLOB");

        modelBuilder.Entity<WorkflowExecutionLogRecord>().Property("SerializedActivityState").HasColumnType("NCLOB");
        modelBuilder.Entity<WorkflowExecutionLogRecord>().Property("SerializedPayload").HasColumnType("NCLOB");

        modelBuilder.Entity<ActivityExecutionRecord>().Property("SerializedActivityState").HasColumnType("NCLOB");
        modelBuilder.Entity<ActivityExecutionRecord>().Property("SerializedException").HasColumnType("NCLOB");
        modelBuilder.Entity<ActivityExecutionRecord>().Property("SerializedPayload").HasColumnType("NCLOB");
        modelBuilder.Entity<ActivityExecutionRecord>().Property("SerializedOutputs").HasColumnType("NCLOB");

        modelBuilder.Entity<StoredBookmark>().Property("SerializedPayload").HasColumnType("NCLOB");
        modelBuilder.Entity<StoredBookmark>().Property("SerializedMetadata").HasColumnType("NCLOB");

        modelBuilder.Entity<WorkflowInboxMessage>().Property("SerializedInput").HasColumnType("NCLOB");
        modelBuilder.Entity<WorkflowInboxMessage>().Property("SerializedBookmarkPayload").HasColumnType("NCLOB");

        modelBuilder.Entity<SerializedKeyValuePair>().Property("SerializedValue").HasColumnType("NCLOB");
    }
}