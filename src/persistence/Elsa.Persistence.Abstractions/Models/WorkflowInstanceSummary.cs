using Elsa.Persistence.Entities;

namespace Elsa.Persistence.Models;

public record WorkflowInstanceSummary(
    string Id,
    string DefinitionId,
    string DefinitionVersionId,
    int Version,
    WorkflowStatus WorkflowStatus,
    string CorrelationId,
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
        workflowInstance.WorkflowStatus,
        workflowInstance.CorrelationId,
        workflowInstance.Name,
        workflowInstance.CreatedAt,
        workflowInstance.LastExecutedAt,
        workflowInstance.FinishedAt,
        workflowInstance.CancelledAt,
        workflowInstance.FaultedAt
    );
}