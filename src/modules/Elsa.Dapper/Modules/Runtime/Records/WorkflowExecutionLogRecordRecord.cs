using Elsa.Dapper.Records;

namespace Elsa.Dapper.Modules.Runtime.Records;

internal class WorkflowExecutionLogRecordRecord : Record
{
    public string Id { get; set; } = null!;
    public string WorkflowDefinitionId { get; set; } = null!;
    public string WorkflowDefinitionVersionId { get; set; } = null!;
    public string WorkflowInstanceId { get; set; } = null!;
    public int WorkflowVersion { get; set; }
    public string ActivityInstanceId { get; set; } = null!;
    public string? ParentActivityInstanceId { get; set; }
    public string ActivityId { get; set; } = null!;
    public string ActivityType { get; set; } = null!;
    public int ActivityTypeVersion { get; set; }
    public string? ActivityName { get; set; }
    public string ActivityNodeId { get; set; } = null!;
    public DateTimeOffset Timestamp { get; set; }
    public long Sequence { get; set; }
    public string? EventName { get; set; }
    public string? Message { get; set; }
    public string? Source { get; set; }
    public string? SerializedActivityState { get; set; }
    public string? SerializedPayload { get; set; }
}