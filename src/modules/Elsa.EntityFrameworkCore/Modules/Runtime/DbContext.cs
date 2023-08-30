using Elsa.EntityFrameworkCore.Common;
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
    /// The workflow inbox messages.
    /// </summary>
    public DbSet<WorkflowInboxMessage> WorkflowInboxMessages { get; set; } = default!;

    /// <inheritdoc />
    protected override void ApplyEntityConfigurations(ModelBuilder modelBuilder)
    {
        var config = new Configurations();
        modelBuilder.ApplyConfiguration<StoredTrigger>(config);
        modelBuilder.ApplyConfiguration<WorkflowExecutionLogRecord>(config);
        modelBuilder.ApplyConfiguration<ActivityExecutionRecord>(config);
        modelBuilder.ApplyConfiguration<StoredBookmark>(config);
        modelBuilder.ApplyConfiguration<WorkflowInboxMessage>(config);
    }
}