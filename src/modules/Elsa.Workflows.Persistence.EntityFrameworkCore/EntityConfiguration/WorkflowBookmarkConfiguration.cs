using Elsa.Workflows.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.Workflows.Persistence.EntityFrameworkCore.EntityConfiguration
{
    public class WorkflowBookmarkConfiguration : IEntityTypeConfiguration<WorkflowBookmark>
    {
        public void Configure(EntityTypeBuilder<WorkflowBookmark> builder)
        {
            builder.HasIndex(x => x.Name).HasDatabaseName($"IX_{nameof(WorkflowBookmark)}_{nameof(WorkflowBookmark.Name)}");
            builder.HasIndex(x => x.Hash).HasDatabaseName($"IX_{nameof(WorkflowBookmark)}_{nameof(WorkflowBookmark.Hash)}");
            builder.HasIndex(x => x.ActivityId).HasDatabaseName($"IX_{nameof(WorkflowBookmark)}_{nameof(WorkflowBookmark.ActivityId)}");
            builder.HasIndex(x => x.WorkflowInstanceId).HasDatabaseName($"IX_{nameof(WorkflowBookmark)}_{nameof(WorkflowBookmark.WorkflowInstanceId)}");
            builder.HasIndex(x => x.CorrelationId).HasDatabaseName($"IX_{nameof(WorkflowBookmark)}_{nameof(WorkflowBookmark.CorrelationId)}");
            builder.HasIndex(x => x.WorkflowDefinitionId).HasDatabaseName($"IX_{nameof(WorkflowBookmark)}_{nameof(WorkflowBookmark.WorkflowDefinitionId)}");
        }
    }
}
