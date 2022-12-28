using Elsa.Workflows.Sink.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.Persistence.EntityFrameworkCore.Modules.WorkflowSink
{
    public class Configurations :
        IEntityTypeConfiguration<WorkflowSinkEntity>
    {
        public void Configure(EntityTypeBuilder<WorkflowSinkEntity> builder)
        {
            builder.Property<string>("Data");
            
            builder.HasIndex(x => x.CreatedAt).HasDatabaseName($"IX_{nameof(WorkflowSink)}_{nameof(WorkflowSinkEntity.CreatedAt)}");
            builder.HasIndex(x => x.LastExecutedAt).HasDatabaseName($"IX_{nameof(WorkflowSink)}_{nameof(WorkflowSinkEntity.LastExecutedAt)}");
            builder.HasIndex(x => x.FinishedAt).HasDatabaseName($"IX_{nameof(WorkflowSink)}_{nameof(WorkflowSinkEntity.FinishedAt)}");
            builder.HasIndex(x => x.FaultedAt).HasDatabaseName($"IX_{nameof(WorkflowSink)}_{nameof(WorkflowSinkEntity.FaultedAt)}");
            builder.HasIndex(x => x.CancelledAt).HasDatabaseName($"IX_{nameof(WorkflowSink)}_{nameof(WorkflowSinkEntity.CancelledAt)}");
        }
    }
}