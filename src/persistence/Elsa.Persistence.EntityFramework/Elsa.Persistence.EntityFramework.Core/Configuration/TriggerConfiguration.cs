using Elsa.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.Persistence.EntityFramework.Core.Configuration;

public class TriggerConfiguration : IEntityTypeConfiguration<Trigger>
{
    public void Configure(EntityTypeBuilder<Trigger> builder)
    {
        builder.HasIndex(x => new { x.ActivityType, x.TenantId, x.Hash}).HasDatabaseName($"IX_{nameof(Trigger)}_{nameof(Trigger.ActivityType)}_{nameof(Trigger.TenantId)}_{nameof(Trigger.Hash)}");
        builder.HasIndex(x => x.TenantId).HasDatabaseName($"IX_{nameof(Trigger)}_{nameof(Trigger.TenantId)}");
        builder.HasIndex(x => x.Hash).HasDatabaseName($"IX_{nameof(Trigger)}_{nameof(Trigger.Hash)}");
        builder.HasIndex(x => x.ActivityType).HasDatabaseName($"IX_{nameof(Trigger)}_{nameof(Trigger.ActivityType)}");
        builder.HasIndex(x => x.ActivityId).HasDatabaseName($"IX_{nameof(Trigger)}_{nameof(Trigger.ActivityId)}");
        builder.HasIndex(x => x.WorkflowDefinitionId).HasDatabaseName($"IX_{nameof(Trigger)}_{nameof(Trigger.WorkflowDefinitionId)}");
        builder.HasIndex(x => new {x.Hash, x.TenantId}).HasDatabaseName($"IX_{nameof(Trigger)}_{nameof(Trigger.Hash)}_{nameof(Trigger.TenantId)}");
    }
}