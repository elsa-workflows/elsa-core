using Elsa.Labels.Entities;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore.Modules.Labels;

public class LabelsElsaDbContext : ElsaDbContextBase
{
    public LabelsElsaDbContext(DbContextOptions<LabelsElsaDbContext> options, IServiceProvider serviceProvider) : base(options, serviceProvider)
    {
    }

    public DbSet<Label> Labels { get; set; } = null!;
    public DbSet<WorkflowDefinitionLabel> WorkflowDefinitionLabels { get; set; } = null!;

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        var config = new Configurations();
        modelBuilder.ApplyConfiguration<Label>(config);
        modelBuilder.ApplyConfiguration<WorkflowDefinitionLabel>(config);
    }
}