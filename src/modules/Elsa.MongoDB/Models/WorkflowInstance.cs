using Elsa.MongoDB.Common;
using Elsa.Workflows.Core.Models;

namespace Elsa.MongoDB.Models;

public class WorkflowInstance : MongoDocument
{
    public string DefinitionId { get; set; } = default!;
    public string DefinitionVersionId { get; set; } = default!;
    public int Version { get; set; }
    public WorkflowStatus Status { get; set; }
    public WorkflowSubStatus SubStatus { get; set; }
    public string? CorrelationId { get; set; }
    public string? Name { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastExecutedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public DateTimeOffset? FaultedAt { get; set; }
    public string Data { get; set; } = default!;
}