using Elsa.EntityFrameworkCore.Common;
using Elsa.Labels.Entities;
using Microsoft.EntityFrameworkCore;

namespace Elsa.EntityFrameworkCore.Modules.Labels;

public class LabelsElsaDbContext(DbContextOptions<LabelsElsaDbContext> options, IServiceProvider serviceProvider) : ElsaDbContextBase(options, serviceProvider)
{
    public DbSet<Label> Labels { get; set; } = default!;
    public DbSet<WorkflowDefinitionLabel> WorkflowDefinitionLabels { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        var config = new Configurations();
        modelBuilder.ApplyConfiguration<Label>(config);
        modelBuilder.ApplyConfiguration<WorkflowDefinitionLabel>(config);
    }
}