using Elsa.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.Persistence.EntityFrameworkCore.Configuration
{
    public class WorkflowInstanceConfiguration : IEntityTypeConfiguration<WorkflowInstance>
    {
        public void Configure(EntityTypeBuilder<WorkflowInstance> builder)
        {
            builder.Ignore(x => x.WorkflowState);
            builder.Property<string>("Data");
            builder.HasIndex(x => new { x.WorkflowStatus, x.DefinitionId, x.Version }).HasDatabaseName($"IX_{nameof(WorkflowInstance)}_{nameof(WorkflowInstance.WorkflowStatus)}_{nameof(WorkflowInstance.DefinitionId)}_{nameof(WorkflowInstance.Version)}");
            builder.HasIndex(x => new { x.WorkflowStatus, x.DefinitionId }).HasDatabaseName($"IX_{nameof(WorkflowInstance)}_{nameof(WorkflowInstance.WorkflowStatus)}_{nameof(WorkflowInstance.DefinitionId)}");
            builder.HasIndex(x => x.DefinitionId).HasDatabaseName($"IX_{nameof(WorkflowInstance)}_{nameof(WorkflowInstance.DefinitionId)}");
            builder.HasIndex(x => x.WorkflowStatus).HasDatabaseName($"IX_{nameof(WorkflowInstance)}_{nameof(WorkflowInstance.WorkflowStatus)}");
            builder.HasIndex(x => x.CorrelationId).HasDatabaseName($"IX_{nameof(WorkflowInstance)}_{nameof(WorkflowInstance.CorrelationId)}");
            builder.HasIndex(x => x.Name).HasDatabaseName($"IX_{nameof(WorkflowInstance)}_{nameof(WorkflowInstance.Name)}");
            builder.HasIndex(x => x.CreatedAt).HasDatabaseName($"IX_{nameof(WorkflowInstance)}_{nameof(WorkflowInstance.CreatedAt)}");
            builder.HasIndex(x => x.LastExecutedAt).HasDatabaseName($"IX_{nameof(WorkflowInstance)}_{nameof(WorkflowInstance.LastExecutedAt)}");
            builder.HasIndex(x => x.FinishedAt).HasDatabaseName($"IX_{nameof(WorkflowInstance)}_{nameof(WorkflowInstance.FinishedAt)}");
            builder.HasIndex(x => x.FaultedAt).HasDatabaseName($"IX_{nameof(WorkflowInstance)}_{nameof(WorkflowInstance.FaultedAt)}");
        }
    }
}