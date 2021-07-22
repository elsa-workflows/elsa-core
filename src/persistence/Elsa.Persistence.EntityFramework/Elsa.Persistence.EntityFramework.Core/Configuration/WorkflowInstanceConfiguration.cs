using Elsa.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.Persistence.EntityFramework.Core.Configuration
{
    public class WorkflowInstanceConfiguration : IEntityTypeConfiguration<WorkflowInstance>
    {
        public void Configure(EntityTypeBuilder<WorkflowInstance> builder)
        {
            builder.Property(x => x.CreatedAt).HasConversion(ValueConverters.InstantConverter);
            builder.Property(x => x.LastExecutedAt).HasConversion(ValueConverters.InstantConverter);
            builder.Property(x => x.FinishedAt).HasConversion(ValueConverters.InstantConverter);
            builder.Property(x => x.CancelledAt).HasConversion(ValueConverters.InstantConverter);
            builder.Property(x => x.FaultedAt).HasConversion(ValueConverters.InstantConverter);
            builder.Ignore(x => x.Input);
            builder.Ignore(x => x.Output);
            builder.Ignore(x => x.ActivityData);
            builder.Ignore(x => x.BlockingActivities);
            builder.Ignore(x => x.Fault);
            builder.Ignore(x => x.ScheduledActivities);
            builder.Ignore(x => x.Scopes);
            builder.Ignore(x => x.Variables);
            builder.Ignore(x => x.CurrentActivity);
            builder.Property<string>("Data");
            builder.HasIndex(x => new { x.WorkflowStatus, x.DefinitionId, x.Version }).HasDatabaseName($"IX_{nameof(WorkflowInstance)}_{nameof(WorkflowInstance.WorkflowStatus)}_{nameof(WorkflowInstance.DefinitionId)}_{nameof(WorkflowInstance.Version)}");
            builder.HasIndex(x => new { x.WorkflowStatus, x.DefinitionId }).HasDatabaseName($"IX_{nameof(WorkflowInstance)}_{nameof(WorkflowInstance.WorkflowStatus)}_{nameof(WorkflowInstance.DefinitionId)}");
            builder.HasIndex(x => x.TenantId).HasDatabaseName($"IX_{nameof(WorkflowInstance)}_{nameof(WorkflowInstance.TenantId)}");
            builder.HasIndex(x => x.DefinitionId).HasDatabaseName($"IX_{nameof(WorkflowInstance)}_{nameof(WorkflowInstance.DefinitionId)}");
            builder.HasIndex(x => x.WorkflowStatus).HasDatabaseName($"IX_{nameof(WorkflowInstance)}_{nameof(WorkflowInstance.WorkflowStatus)}");
            builder.HasIndex(x => x.CorrelationId).HasDatabaseName($"IX_{nameof(WorkflowInstance)}_{nameof(WorkflowInstance.CorrelationId)}");
            builder.HasIndex(x => x.ContextType).HasDatabaseName($"IX_{nameof(WorkflowInstance)}_{nameof(WorkflowInstance.ContextType)}");
            builder.HasIndex(x => x.ContextId).HasDatabaseName($"IX_{nameof(WorkflowInstance)}_{nameof(WorkflowInstance.ContextId)}");
            builder.HasIndex(x => x.Name).HasDatabaseName($"IX_{nameof(WorkflowInstance)}_{nameof(WorkflowInstance.Name)}");
            builder.HasIndex(x => x.CreatedAt).HasDatabaseName($"IX_{nameof(WorkflowInstance)}_{nameof(WorkflowInstance.CreatedAt)}");
            builder.HasIndex(x => x.LastExecutedAt).HasDatabaseName($"IX_{nameof(WorkflowInstance)}_{nameof(WorkflowInstance.LastExecutedAt)}");
            builder.HasIndex(x => x.FinishedAt).HasDatabaseName($"IX_{nameof(WorkflowInstance)}_{nameof(WorkflowInstance.FinishedAt)}");
            builder.HasIndex(x => x.FaultedAt).HasDatabaseName($"IX_{nameof(WorkflowInstance)}_{nameof(WorkflowInstance.FaultedAt)}");
        }
    }
}