using Elsa.Persistence.EntityFrameworkCore.Common;
using Elsa.Workflows.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Workflows.Persistence.EntityFrameworkCore.DbContexts;

public class ElsaDbContext : ElsaDbContextBase
{
    public ElsaDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<WorkflowDefinition> WorkflowDefinitions { get; set; } = default!;
    public DbSet<WorkflowInstance> WorkflowInstances { get; set; } = default!;
    public DbSet<WorkflowTrigger> WorkflowTriggers { get; set; } = default!;
    public DbSet<WorkflowBookmark> WorkflowBookmarks { get; set; } = default!;
    public DbSet<WorkflowExecutionLogRecord> WorkflowExecutionLogRecords { get; set; } = default!;


    protected override void SetupForOracle(ModelBuilder modelBuilder)
    {
        // In order to use data more than 2000 char we have to use NCLOB.
        // In oracle we have to explicitly say the column is NCLOB otherwise it would be considered nvarchar(2000).
        modelBuilder.Entity<WorkflowInstance>().Property("Data").HasColumnType("NCLOB");
        modelBuilder.Entity<WorkflowDefinition>().Property("Data").HasColumnType("NCLOB");
        modelBuilder.Entity<WorkflowBookmark>().Property("Data").HasColumnType("NCLOB");
    }
}