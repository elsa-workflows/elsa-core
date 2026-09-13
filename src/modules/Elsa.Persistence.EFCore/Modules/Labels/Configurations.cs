using Elsa.Labels.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.Persistence.EFCore.Modules.Labels;

public class Configurations : IEntityTypeConfiguration<Label>, IEntityTypeConfiguration<WorkflowDefinitionLabel>
{
    public void Configure(EntityTypeBuilder<Label> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(Label.NameMaxLength).IsRequired();
        builder.Property(x => x.NormalizedName).HasMaxLength(Label.NameMaxLength).IsRequired();

        // Uniqueness is per tenant, matching Secrets (and User, Role, Application).
        // A global unique index would make the name a shared resource across tenants.
        // SQL Server/Oracle still emit a filtered unique index (TenantId IS NOT NULL).
        // Default-tenant rows use "" — migrations stamp leftover null TenantId to "" first.
        builder.HasIndex(x => new { x.TenantId, x.NormalizedName })
            .HasDatabaseName($"IX_{nameof(Label)}_{nameof(Label.TenantId)}_{nameof(Label.NormalizedName)}")
            .IsUnique();
    }

    public void Configure(EntityTypeBuilder<WorkflowDefinitionLabel> builder)
    {
        builder.HasIndex(x => x.WorkflowDefinitionId).HasDatabaseName($"{nameof(WorkflowDefinitionLabel)}_{nameof(WorkflowDefinitionLabel.WorkflowDefinitionId)}");
        builder.HasIndex(x => x.WorkflowDefinitionVersionId).HasDatabaseName($"{nameof(WorkflowDefinitionLabel)}_{nameof(WorkflowDefinitionLabel.WorkflowDefinitionVersionId)}");
        builder.HasIndex(x => x.LabelId).HasDatabaseName($"{nameof(WorkflowDefinitionLabel)}_{nameof(WorkflowDefinitionLabel.LabelId)}");
        builder.HasIndex(x => x.TenantId).HasDatabaseName($"{nameof(WorkflowDefinitionLabel)}_{nameof(WorkflowDefinitionLabel.TenantId)}");
    }
}