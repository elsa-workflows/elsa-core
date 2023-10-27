using Elsa.EntityFrameworkCore.Common;
using Elsa.Labels.Entities;
using Microsoft.EntityFrameworkCore;

namespace Elsa.EntityFrameworkCore.Modules.Labels;

public class LabelsElsaDbContext : ElsaDbContextBase
{
    public LabelsElsaDbContext(DbContextOptions options) : base(options)
    {
        var elsaDbContextOptions = options.FindExtension<ElsaDbContextOptionsExtension>()?.Options;
        _additionnalEntityConfigurations = elsaDbContextOptions?.AdditionnalEntityConfigurations;
        _serviceProvider = serviceProvider;
    }

    private readonly Action<ModelBuilder, IServiceProvider>? _additionnalEntityConfigurations;
    private readonly IServiceProvider _serviceProvider;

    public DbSet<Label> Labels { get; set; } = default!;
    public DbSet<WorkflowDefinitionLabel> WorkflowDefinitionLabels { get; set; } = default!;

    protected override void ApplyEntityConfigurations(ModelBuilder modelBuilder)
    {
        var config = new Configurations();
        modelBuilder.ApplyConfiguration<Label>(config);
        modelBuilder.ApplyConfiguration<WorkflowDefinitionLabel>(config);
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _additionnalEntityConfigurations?.Invoke(modelBuilder, _serviceProvider);

        base.OnModelCreating(modelBuilder);
    }
}