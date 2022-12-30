using Elsa.Persistence.EntityFrameworkCore.Common;
using Elsa.Workflows.Sinks.Models;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EntityFrameworkCore.Modules.WorkflowSink;

public class WorkflowSinkElsaDbContext : ElsaDbContextBase
{
    public WorkflowSinkElsaDbContext(DbContextOptions options) : base(options)
    {
    }
    
    public DbSet<WorkflowInstance> WorkflowSink { get; set; } = default!;

    protected override void ApplyEntityConfigurations(ModelBuilder modelBuilder)
    {
        var config = new Configurations();
        modelBuilder.ApplyConfiguration(config);
    }
}