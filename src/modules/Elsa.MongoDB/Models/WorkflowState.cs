using Elsa.MongoDB.Common;
using Elsa.Workflows.Core.Models;

namespace Elsa.MongoDB.Models;

public class WorkflowState : MongoDocument
{
    public string DefinitionId { get; set; } = default!;
    public int DefinitionVersion { get; set; } = default!;
    public string? CorrelationId { get; set; }
    public WorkflowStatus Status { get; set; }
    public WorkflowSubStatus SubStatus { get; set; }
    public string Data { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}