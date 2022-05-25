using Elsa.Labels.Entities;
using Elsa.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.Labels.EntityFrameworkCore.Configuration;

public class WorkflowDefinitionLabelConfiguration : IEntityTypeConfiguration<WorkflowDefinitionLabel>
{
    public void Configure(EntityTypeBuilder<WorkflowTrigger> builder)
    {
        builder.HasIndex(x => x.WorkflowDefinitionId).HasDatabaseName($"IX_{nameof(WorkflowTrigger)}_{nameof(WorkflowTrigger.WorkflowDefinitionId)}");
        builder.HasIndex(x => x.Name).HasDatabaseName($"IX_{nameof(WorkflowTrigger)}_{nameof(WorkflowTrigger.Name)}");
        builder.HasIndex(x => x.Hash).HasDatabaseName($"IX_{nameof(WorkflowTrigger)}_{nameof(WorkflowTrigger.Hash)}");
    }

    public void Configure(EntityTypeBuilder<WorkflowDefinitionLabel> builder)
    {
        builder.HasIndex(x => x.WorkflowDefinitionId).HasDatabaseName($"{nameof(WorkflowDefinitionLabel)}_{nameof(WorkflowDefinitionLabel.WorkflowDefinitionId)}");
        builder.HasIndex(x => x.WorkflowDefinitionVersionId).HasDatabaseName($"{nameof(WorkflowDefinitionLabel)}_{nameof(WorkflowDefinitionLabel.WorkflowDefinitionVersionId)}");
        builder.HasIndex(x => x.LabelId).HasDatabaseName($"{nameof(WorkflowDefinitionLabel)}_{nameof(WorkflowDefinitionLabel.LabelId)}");
    }
}