using Elsa.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.Persistence.EntityFramework.Core.Configuration
{
    public class WorkflowDefinitionConfiguration : IEntityTypeConfiguration<WorkflowDefinition>
    {
        public void Configure(EntityTypeBuilder<WorkflowDefinition> builder)
        {
            builder.Ignore(x => x.Activities);
            builder.Ignore(x => x.Connections);
            builder.Ignore(x => x.Variables);
            builder.Ignore(x => x.CustomAttributes);
            builder.Ignore(x => x.ContextOptions);
            builder.Ignore(x => x.Channel);
            builder.Property<string>("Data");
            builder.HasIndex(x => new {x.DefinitionId, x.Version}).HasDatabaseName($"IX_{nameof(WorkflowDefinition)}_{nameof(WorkflowDefinition.DefinitionId)}_{nameof(WorkflowDefinition.VersionId)}").IsUnique();
            builder.HasIndex(x => x.TenantId).HasDatabaseName($"IX_{nameof(WorkflowDefinition)}_{nameof(WorkflowDefinition.TenantId)}");
            builder.HasIndex(x => x.Version).HasDatabaseName($"IX_{nameof(WorkflowDefinition)}_{nameof(WorkflowDefinition.Version)}");
            builder.HasIndex(x => x.Name).HasDatabaseName($"IX_{nameof(WorkflowDefinition)}_{nameof(WorkflowDefinition.Name)}");
            builder.HasIndex(x => x.IsLatest).HasDatabaseName($"IX_{nameof(WorkflowDefinition)}_{nameof(WorkflowDefinition.IsLatest)}");
            builder.HasIndex(x => x.IsPublished).HasDatabaseName($"IX_{nameof(WorkflowDefinition)}_{nameof(WorkflowDefinition.IsPublished)}");
            builder.HasIndex(x => x.Tag).HasDatabaseName($"IX_{nameof(WorkflowDefinition)}_{nameof(WorkflowDefinition.Tag)}");
        }
    }
}