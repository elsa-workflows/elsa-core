using Elsa.CustomActivities.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.CustomActivities.EntityFrameworkCore.EntityConfiguration;

public class ActivityDefinitionConfiguration : IEntityTypeConfiguration<ActivityDefinition>
{
    public void Configure(EntityTypeBuilder<ActivityDefinition> builder)
    {
        builder.Ignore(x => x.Variables);
        builder.Ignore(x => x.Metadata);
        builder.Ignore(x => x.ApplicationProperties);
        builder.Property<string>("Data");

        builder.HasIndex(x => new {x.DefinitionId, x.Version}).HasDatabaseName($"IX_{nameof(ActivityDefinition)}_{nameof(ActivityDefinition.DefinitionId)}_{nameof(ActivityDefinition.Version)}").IsUnique();
        builder.HasIndex(x => x.Version).HasDatabaseName($"IX_{nameof(ActivityDefinition)}_{nameof(ActivityDefinition.Version)}");
        builder.HasIndex(x => x.Name).HasDatabaseName($"IX_{nameof(ActivityDefinition)}_{nameof(ActivityDefinition.Name)}");
        builder.HasIndex(x => x.IsLatest).HasDatabaseName($"IX_{nameof(ActivityDefinition)}_{nameof(ActivityDefinition.IsLatest)}");
        builder.HasIndex(x => x.IsPublished).HasDatabaseName($"IX_{nameof(ActivityDefinition)}_{nameof(ActivityDefinition.IsPublished)}");
    }
}