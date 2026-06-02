using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Models;

namespace Elsa.Workflows.Management.Persistence.ModularPersistence.Storage;

public sealed class WorkflowInstanceMetadataRecord
{
    public string Id { get; set; } = default!;
    public string? TenantId { get; set; }
    public string DefinitionId { get; set; } = default!;
    public string DefinitionVersionId { get; set; } = default!;
    public int Version { get; set; }
    public string? ParentWorkflowInstanceId { get; set; }
    public WorkflowStatus Status { get; set; }
    public WorkflowSubStatus SubStatus { get; set; }
    public bool IsExecuting { get; set; }
    public string? CorrelationId { get; set; }
    public string? Name { get; set; }
    public int IncidentCount { get; set; }
    public bool IsSystem { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }

    public static WorkflowInstanceMetadataRecord FromInstance(WorkflowInstance instance) =>
        new()
        {
            Id = instance.Id,
            TenantId = instance.TenantId,
            DefinitionId = instance.DefinitionId,
            DefinitionVersionId = instance.DefinitionVersionId,
            Version = instance.Version,
            ParentWorkflowInstanceId = instance.ParentWorkflowInstanceId,
            Status = instance.Status,
            SubStatus = instance.SubStatus,
            IsExecuting = instance.IsExecuting,
            CorrelationId = instance.CorrelationId,
            Name = instance.Name,
            IncidentCount = instance.IncidentCount,
            IsSystem = instance.IsSystem,
            CreatedAt = instance.CreatedAt,
            UpdatedAt = instance.UpdatedAt,
            FinishedAt = instance.FinishedAt
        };

    public WorkflowInstanceSummary ToSummary() =>
        new()
        {
            Id = Id,
            TenantId = TenantId,
            DefinitionId = DefinitionId,
            DefinitionVersionId = DefinitionVersionId,
            Version = Version,
            Status = Status,
            SubStatus = SubStatus,
            CorrelationId = CorrelationId,
            Name = Name,
            IncidentCount = IncidentCount,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
            FinishedAt = FinishedAt
        };
}
