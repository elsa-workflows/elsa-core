namespace Elsa.Dapper.Modules.Runtime.Records;

internal class WorkflowExecutionLogRecordRecord
{
    public string Id { get; set; } = default!;
    public string WorkflowDefinitionId { get; set; } = default!;
    public string WorkflowDefinitionVersionId { get; set; } = default!;
    public string WorkflowInstanceId { get; set; } = default!;
    public int WorkflowVersion { get; set; }
    public string ActivityInstanceId { get; set; } = default!;
    public string? ParentActivityInstanceId { get; set; }
    public string ActivityId { get; set; } = default!;
    public string ActivityType { get; set; } = default!;
    public int ActivityTypeVersion { get; set; }
    public string? ActivityName { get; set; } = default!;
    public string NodeId { get; set; } = default!;
    public DateTimeOffset Timestamp { get; set; }
    public long Sequence { get; set; }
    public string? EventName { get; set; }
    public string? Message { get; set; }
    public string? Source { get; set; }
    public string? SerializedActivityState { get; set; }
    public string? SerializedPayload { get; set; }
}