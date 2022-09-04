using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Management.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Elsa.Workflows.Persistence.EntityFrameworkCore.EntityConfiguration
{
    public class WorkflowStateConfiguration : IEntityTypeConfiguration<WorkflowState>
    {
        public void Configure(EntityTypeBuilder<WorkflowInstance> builder)
        {
            builder.Ignore(x => x.WorkflowState);
            builder.Ignore(x => x.Fault);
            builder.Property<string>("Data");
            builder.Property(x => x.Status).HasConversion<EnumToStringConverter<WorkflowStatus>>();
            builder.Property(x => x.SubStatus).HasConversion<EnumToStringConverter<WorkflowSubStatus>>();
            builder.HasIndex(x => new { x.Status, x.SubStatus, x.DefinitionId, x.Version }).HasDatabaseName($"IX_{nameof(WorkflowInstance)}_{nameof(WorkflowInstance.Status)}_{nameof(WorkflowInstance.SubStatus)}_{nameof(WorkflowInstance.DefinitionId)}_{nameof(WorkflowInstance.Version)}");
            builder.HasIndex(x => new { x.Status, x.SubStatus }).HasDatabaseName($"IX_{nameof(WorkflowInstance)}_{nameof(WorkflowInstance.Status)}_{nameof(WorkflowInstance.SubStatus)}");
            builder.HasIndex(x => new { x.Status, x.DefinitionId }).HasDatabaseName($"IX_{nameof(WorkflowInstance)}_{nameof(WorkflowInstance.Status)}_{nameof(WorkflowInstance.DefinitionId)}");
            builder.HasIndex(x => new { x.SubStatus, x.DefinitionId }).HasDatabaseName($"IX_{nameof(WorkflowInstance)}_{nameof(WorkflowInstance.SubStatus)}_{nameof(WorkflowInstance.DefinitionId)}");
            builder.HasIndex(x => x.DefinitionId).HasDatabaseName($"IX_{nameof(WorkflowInstance)}_{nameof(WorkflowInstance.DefinitionId)}");
            builder.HasIndex(x => x.Status).HasDatabaseName($"IX_{nameof(WorkflowInstance)}_{nameof(WorkflowInstance.Status)}");
            builder.HasIndex(x => x.SubStatus).HasDatabaseName($"IX_{nameof(WorkflowInstance)}_{nameof(WorkflowInstance.SubStatus)}");
            builder.HasIndex(x => x.CorrelationId).HasDatabaseName($"IX_{nameof(WorkflowInstance)}_{nameof(WorkflowInstance.CorrelationId)}");
            builder.HasIndex(x => x.Name).HasDatabaseName($"IX_{nameof(WorkflowInstance)}_{nameof(WorkflowInstance.Name)}");
            builder.HasIndex(x => x.CreatedAt).HasDatabaseName($"IX_{nameof(WorkflowInstance)}_{nameof(WorkflowInstance.CreatedAt)}");
            builder.HasIndex(x => x.LastExecutedAt).HasDatabaseName($"IX_{nameof(WorkflowInstance)}_{nameof(WorkflowInstance.LastExecutedAt)}");
            builder.HasIndex(x => x.FinishedAt).HasDatabaseName($"IX_{nameof(WorkflowInstance)}_{nameof(WorkflowInstance.FinishedAt)}");
            builder.HasIndex(x => x.FaultedAt).HasDatabaseName($"IX_{nameof(WorkflowInstance)}_{nameof(WorkflowInstance.FaultedAt)}");
        }

        public void Configure(EntityTypeBuilder<WorkflowState> builder)
        {
            builder.Ignore(x => x.Bookmarks);
            builder.Ignore(x => x.Properties);
            builder.Ignore(x => x.ActivityOutput);
            builder.Ignore(x => x.CompletionCallbacks);
            builder.Ignore(x => x.PersistentVariables);
            builder.Ignore(x => x.WorkflowIdentity);
            builder.Ignore(x => x.ActivityExecutionContexts);
            builder.Property<string>("DefinitionId");
            builder.Property<int>("Version");
            builder.Property<string>("Data");
            builder.Property<DateTimeOffset>("CreatedAt");
            builder.Property<DateTimeOffset>("UpdatedAt");
            builder.Property(x => x.Status).HasConversion<EnumToStringConverter<WorkflowStatus>>();
            builder.Property(x => x.SubStatus).HasConversion<EnumToStringConverter<WorkflowSubStatus>>();
            builder.HasIndex(x => x.CorrelationId).HasDatabaseName($"IX_{nameof(WorkflowState)}_{nameof(WorkflowState.CorrelationId)}");
            builder.HasIndex("DefinitionId").HasDatabaseName($"IX_{nameof(WorkflowState)}_DefinitionId");
            builder.HasIndex("Status", "SubStatus", "DefinitionId", "Version").HasDatabaseName($"IX_{nameof(WorkflowInstance)}_Status_SubStatus_DefinitionId_Version");
            builder.HasIndex("Status", "SubStatus").HasDatabaseName($"IX_{nameof(WorkflowInstance)}_Status_SubStatus");
            builder.HasIndex("Status", "DefinitionId").HasDatabaseName($"IX_{nameof(WorkflowInstance)}_Status_DefinitionId");
            builder.HasIndex(x => new { x.Status, x.SubStatus }).HasDatabaseName($"IX_{nameof(WorkflowInstance)}_{nameof(WorkflowInstance.Status)}_{nameof(WorkflowInstance.SubStatus)}");
            builder.HasIndex("CreatedAt").HasDatabaseName($"IX_{nameof(WorkflowInstance)}_CreatedAt");
            builder.HasIndex("UpdatedAt").HasDatabaseName($"IX_{nameof(WorkflowInstance)}_UpdatedAt");
        }
    }
}