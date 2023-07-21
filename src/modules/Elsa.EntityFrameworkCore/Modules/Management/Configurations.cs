using System.Linq.Expressions;
using Elsa.Workflows.Core;
using Elsa.Workflows.Management.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Elsa.EntityFrameworkCore.Modules.Management;

internal class Configurations : IEntityTypeConfiguration<WorkflowDefinition>, IEntityTypeConfiguration<WorkflowInstance>
{
    private static Expression<Func<Version?, string?>> VersionToStringConverter => v => v != null ? v.ToString() : null;
    private static Expression<Func<string?, Version?>> StringToVersionConverter => v => v != null ? Version.Parse(v) : null;
    
    public void Configure(EntityTypeBuilder<WorkflowDefinition> builder)
    {
        builder.Ignore(x => x.Variables);
        builder.Ignore(x => x.Inputs);
        builder.Ignore(x => x.Outputs);
        builder.Ignore(x => x.Outcomes);
        builder.Ignore(x => x.CustomProperties);
        builder.Ignore(x => x.Options);
        builder.Property<string>("Data");
        builder.Property<bool?>("UsableAsActivity");
        builder.Property(x => x.ToolVersion).HasConversion(VersionToStringConverter, StringToVersionConverter);

        builder.HasIndex(x => new {x.DefinitionId, x.Version}).HasDatabaseName($"IX_{nameof(WorkflowDefinition)}_{nameof(WorkflowDefinition.DefinitionId)}_{nameof(WorkflowDefinition.Version)}").IsUnique();
        builder.HasIndex(x => x.Version).HasDatabaseName($"IX_{nameof(WorkflowDefinition)}_{nameof(WorkflowDefinition.Version)}");
        builder.HasIndex(x => x.Name).HasDatabaseName($"IX_{nameof(WorkflowDefinition)}_{nameof(WorkflowDefinition.Name)}");
        builder.HasIndex(x => x.IsLatest).HasDatabaseName($"IX_{nameof(WorkflowDefinition)}_{nameof(WorkflowDefinition.IsLatest)}");
        builder.HasIndex(x => x.IsPublished).HasDatabaseName($"IX_{nameof(WorkflowDefinition)}_{nameof(WorkflowDefinition.IsPublished)}");
        builder.HasIndex("UsableAsActivity").HasDatabaseName($"IX_{nameof(WorkflowDefinition)}_UsableAsActivity");
    }
        
    public void Configure(EntityTypeBuilder<WorkflowInstance> builder)
    {
        builder.Ignore(x => x.WorkflowState);
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
        builder.HasIndex(x => x.UpdatedAt).HasDatabaseName($"IX_{nameof(WorkflowInstance)}_{nameof(WorkflowInstance.UpdatedAt)}");
        builder.HasIndex(x => x.FinishedAt).HasDatabaseName($"IX_{nameof(WorkflowInstance)}_{nameof(WorkflowInstance.FinishedAt)}");
    }
}