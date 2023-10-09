using Elsa.Alterations.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.EntityFrameworkCore.Modules.Alterations;

/// <summary>
/// EF Core configuration for various entity types. 
/// </summary>
public class Configurations : IEntityTypeConfiguration<AlterationPlan>, IEntityTypeConfiguration<AlterationJob>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AlterationPlan> builder)
    {
        builder.Ignore(x => x.Alterations);
        builder.Ignore(x => x.WorkflowInstanceIds);
        builder.Property<string>("SerializedAlterations");
        builder.Property<string>("SerializedWorkflowInstanceIds");
        builder.HasIndex(x => x.Status).HasDatabaseName($"IX_{nameof(AlterationPlan)}_{nameof(AlterationPlan.Status)}");
        builder.HasIndex(x => x.CreatedAt).HasDatabaseName($"IX_{nameof(AlterationPlan)}_{nameof(AlterationPlan.CreatedAt)}");
        builder.HasIndex(x => x.StartedAt).HasDatabaseName($"IX_{nameof(AlterationPlan)}_{nameof(AlterationPlan.StartedAt)}");
        builder.HasIndex(x => x.CompletedAt).HasDatabaseName($"IX_{nameof(AlterationPlan)}_{nameof(AlterationPlan.CompletedAt)}");
    }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AlterationJob> builder)
    {
        builder.Ignore(x => x.Log);
        builder.Property<string>("SerializedLog");
        builder.HasIndex(x => x.PlanId).HasDatabaseName($"IX_{nameof(AlterationJob)}_{nameof(AlterationJob.PlanId)}");
        builder.HasIndex(x => x.WorkflowInstanceId).HasDatabaseName($"IX_{nameof(AlterationJob)}_{nameof(AlterationJob.WorkflowInstanceId)}");
        builder.HasIndex(x => x.Status).HasDatabaseName($"IX_{nameof(AlterationJob)}_{nameof(AlterationJob.Status)}");
        builder.HasIndex(x => x.CreatedAt).HasDatabaseName($"IX_{nameof(AlterationJob)}_{nameof(AlterationJob.CreatedAt)}");
        builder.HasIndex(x => x.StartedAt).HasDatabaseName($"IX_{nameof(AlterationJob)}_{nameof(AlterationJob.StartedAt)}");
        builder.HasIndex(x => x.CompletedAt).HasDatabaseName($"IX_{nameof(AlterationJob)}_{nameof(AlterationJob.CompletedAt)}");
    }
}