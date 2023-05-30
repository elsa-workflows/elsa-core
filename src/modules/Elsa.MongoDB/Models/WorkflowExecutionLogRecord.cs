using Elsa.MongoDB.Common;

namespace Elsa.MongoDB.Models;

public class WorkflowExecutionLogRecord : MongoDocument
{
    public string WorkflowDefinitionId { get; set; } = default!;
    public string WorkflowInstanceId { get; set; } = default!;
    public int WorkflowVersion { get; init; }
    public string ActivityInstanceId { get; set; } = default!;
    public string? ParentActivityInstanceId { get; set; }
    public string ActivityId { get; set; } = default!;
    public string ActivityType { get; set; } = default!;
    public string NodeId { get; set; } = default!;
    public DateTimeOffset Timestamp { get; set; }
    public string? EventName { get; set; }
    public string? Message { get; set; }
    public string? Source { get; set; }
    public string? ActivityData { get; set; }
    public string? PayloadData { get; set; }
}