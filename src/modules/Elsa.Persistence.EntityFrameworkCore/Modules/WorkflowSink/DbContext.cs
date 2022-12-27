using Elsa.Persistence.EntityFrameworkCore.Common;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Sink.Models;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EntityFrameworkCore.Modules.WorkflowSink;

public class WorkflowSinkElsaDbContext : ElsaDbContextBase
{
    public WorkflowSinkElsaDbContext(DbContextOptions options) : base(options)
    {
    }
    
    public DbSet<Workflows.Sink.Models.WorkflowSink> WorkflowSink { get; set; } = default!;

    protected override void ApplyEntityConfigurations(ModelBuilder modelBuilder)
    {
        var config = new Configurations();
        modelBuilder.ApplyConfiguration(config);
    }
}