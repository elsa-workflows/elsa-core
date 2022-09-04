using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Entities;

namespace Elsa.Workflows.Management.Models;

public record WorkflowInstanceSummary(
    string Id,
    string DefinitionId,
    string DefinitionVersionId,
    int Version,
    WorkflowStatus Status,
    WorkflowSubStatus SubStatus,
    string? CorrelationId,
    string? Name,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastExecutedAt,
    DateTimeOffset? FinishedAt,
    DateTimeOffset? CancelledAt,
    DateTimeOffset? FaultedAt)
{
    public static WorkflowInstanceSummary FromInstance(WorkflowInstance workflowInstance) => new(
        workflowInstance.Id,
        workflowInstance.DefinitionId,
        workflowInstance.DefinitionVersionId,
        workflowInstance.Version,
        workflowInstance.Status,
        workflowInstance.SubStatus,
        workflowInstance.CorrelationId,
        workflowInstance.Name,
        workflowInstance.CreatedAt,
        workflowInstance.LastExecutedAt,
        workflowInstance.FinishedAt,
        workflowInstance.CancelledAt,
        workflowInstance.FaultedAt
    );
}