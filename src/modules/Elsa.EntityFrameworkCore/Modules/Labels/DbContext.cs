using Elsa.EntityFrameworkCore.Common;
using Elsa.Labels.Entities;
using Microsoft.EntityFrameworkCore;

namespace Elsa.EntityFrameworkCore.Modules.Labels;

public class LabelsElsaDbContext : ElsaDbContextBase
{
    public LabelsElsaDbContext(DbContextOptions options) : base(options)
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