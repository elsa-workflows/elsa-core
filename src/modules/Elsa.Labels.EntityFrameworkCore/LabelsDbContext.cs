using Elsa.Labels.Entities;
using Elsa.Persistence.EntityFrameworkCore.Common.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Labels.EntityFrameworkCore;

public class LabelsDbContext : ElsaDbContextBase
{
    public LabelsDbContext(DbContextOptions options) : base(options)
    {
    }
    
    public DbSet<Label> Labels { get; set; } = default!;
    public DbSet<WorkflowDefinitionLabel> WorkflowDefinitionLabels { get; set; } = default!;

    protected override void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LabelsDbContext).Assembly);
    }
}