using System;
using Elsa.State;

namespace Elsa.Persistence.Entities;

public class WorkflowInstance : Entity
{
    public string DefinitionId { get; init; } = default!;
    public string DefinitionVersionId { get; init; } = default!;
    public int Version { get; init; }
    public WorkflowState WorkflowState { get; set; } = default!;
    public WorkflowStatus WorkflowStatus { get; set; }
    public string CorrelationId { get; init; } = default!;
    public string? Name { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastExecutedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? FaultedAt { get; set; }
}