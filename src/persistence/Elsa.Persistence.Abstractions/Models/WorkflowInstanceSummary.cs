using System;
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
    DateTime CreatedAt,
    DateTime? LastExecutedAt,
    DateTime? FinishedAt,
    DateTime? CancelledAt,
    DateTime? FaultedAt)
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