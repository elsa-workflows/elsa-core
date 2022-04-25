using Elsa.Models;
using Elsa.Persistence.Models;
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
    public WorkflowFault? Fault { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastExecutedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public DateTimeOffset? FaultedAt { get; set; }
}