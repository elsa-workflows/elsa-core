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
    public RuntimeElsaDbContext(DbContextOptions<RuntimeElsaDbContext> options, IServiceProvider serviceProvider) : base(options, serviceProvider)
    {
    }
    
    /// The workflow triggers.
    public DbSet<StoredTrigger> Triggers { get; set; } = default!;
    
    /// The workflow execution log records.
    public DbSet<WorkflowExecutionLogRecord> WorkflowExecutionLogRecords { get; set; } = default!;
    
    /// The activity execution records.
    public DbSet<ActivityExecutionRecord> ActivityExecutionRecords { get; set; } = default!;
    
    /// The workflow bookmarks.
    public DbSet<StoredBookmark> Bookmarks { get; set; } = default!;
    
    /// The bookmark queue items.
    public DbSet<BookmarkQueueItem> BookmarkQueueItems { get; set; } = default!;
    
    /// The generic key value pairs.
    public DbSet<SerializedKeyValuePair> KeyValuePairs { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        var config = new Configurations();
        modelBuilder.ApplyConfiguration<StoredTrigger>(config);
        modelBuilder.ApplyConfiguration<WorkflowExecutionLogRecord>(config);
        modelBuilder.ApplyConfiguration<ActivityExecutionRecord>(config);
        modelBuilder.ApplyConfiguration<StoredBookmark>(config);
        modelBuilder.ApplyConfiguration<BookmarkQueueItem>(config);
        modelBuilder.ApplyConfiguration<SerializedKeyValuePair>(config);
    }
}