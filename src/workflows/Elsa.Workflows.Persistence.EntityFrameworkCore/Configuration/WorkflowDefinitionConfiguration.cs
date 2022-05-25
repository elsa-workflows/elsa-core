using Elsa.Workflows.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.Workflows.Persistence.EntityFrameworkCore.Configuration
{
    public class WorkflowDefinitionConfiguration : IEntityTypeConfiguration<WorkflowDefinition>
    {
        public void Configure(EntityTypeBuilder<WorkflowDefinition> builder)
        {
            builder.Ignore(x => x.Variables);
            builder.Ignore(x => x.Tags);
            builder.Ignore(x => x.Metadata);
            builder.Ignore(x => x.ApplicationProperties);
            builder.Property<string>("Data");

            builder.HasIndex(x => new {x.DefinitionId, x.Version}).HasDatabaseName($"IX_{nameof(WorkflowDefinition)}_{nameof(WorkflowDefinition.DefinitionId)}_{nameof(WorkflowDefinition.Version)}").IsUnique();
            builder.HasIndex(x => x.Version).HasDatabaseName($"IX_{nameof(WorkflowDefinition)}_{nameof(WorkflowDefinition.Version)}");
            builder.HasIndex(x => x.Name).HasDatabaseName($"IX_{nameof(WorkflowDefinition)}_{nameof(WorkflowDefinition.Name)}");
            builder.HasIndex(x => x.IsLatest).HasDatabaseName($"IX_{nameof(WorkflowDefinition)}_{nameof(WorkflowDefinition.IsLatest)}");
            builder.HasIndex(x => x.IsPublished).HasDatabaseName($"IX_{nameof(WorkflowDefinition)}_{nameof(WorkflowDefinition.IsPublished)}");
        }
    }
}