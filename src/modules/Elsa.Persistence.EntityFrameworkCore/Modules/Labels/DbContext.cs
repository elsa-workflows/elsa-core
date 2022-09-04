using Elsa.Labels.Entities;
using Elsa.Persistence.EntityFrameworkCore.Common;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EntityFrameworkCore.Modules.Labels;

public class LabelsDbContext : DbContextBase
{
    public LabelsDbContext(DbContextOptions options) : base(options)
    {
    }
    
    public DbSet<Label> Labels { get; set; } = default!;
    public DbSet<WorkflowDefinitionLabel> WorkflowDefinitionLabels { get; set; } = default!;

    protected override void ApplyEntityConfigurations(ModelBuilder modelBuilder)
    {
        var config = new Configurations();
        modelBuilder.ApplyConfiguration<Label>(config);
        modelBuilder.ApplyConfiguration<WorkflowDefinitionLabel>(config);
    }
}