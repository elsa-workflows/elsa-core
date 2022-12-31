using Elsa.Labels.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.EntityFrameworkCore.Modules.Labels;

public class Configurations : IEntityTypeConfiguration<Label>, IEntityTypeConfiguration<WorkflowDefinitionLabel>
{
    public void Configure(EntityTypeBuilder<Label> builder)
    {
    }

    public void Configure(EntityTypeBuilder<WorkflowDefinitionLabel> builder)
    {
        builder.HasIndex(x => x.WorkflowDefinitionId).HasDatabaseName($"{nameof(WorkflowDefinitionLabel)}_{nameof(WorkflowDefinitionLabel.WorkflowDefinitionId)}");
        builder.HasIndex(x => x.WorkflowDefinitionVersionId).HasDatabaseName($"{nameof(WorkflowDefinitionLabel)}_{nameof(WorkflowDefinitionLabel.WorkflowDefinitionVersionId)}");
        builder.HasIndex(x => x.LabelId).HasDatabaseName($"{nameof(WorkflowDefinitionLabel)}_{nameof(WorkflowDefinitionLabel.LabelId)}");
    }
}