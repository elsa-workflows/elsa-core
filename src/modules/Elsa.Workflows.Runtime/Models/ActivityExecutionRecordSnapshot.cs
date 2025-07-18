namespace Elsa.Workflows.Runtime;

public class ActivityExecutionRecordSnapshot
{
    public string Id { get; set; } = null!;
    public string? TenantId { get; set; }
    public string WorkflowInstanceId { get; set; } = null!;
    public string ActivityId { get; set; } = null!;
    public string ActivityNodeId { get; set; } = null!;
    public string ActivityType { get; set; } = null!;
    public int ActivityTypeVersion { get; set; }
    public string? ActivityName { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public bool HasBookmarks { get; set; }
    public ActivityStatus Status { get; set; }
    public int AggregateFaultCount { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? SerializedActivityState { get; set; }
    public string? SerializedOutputs { get; set; }
    public string? SerializedProperties { get; set; }
    public string? SerializedPayload { get; set; }
    public string? SerializedMetadata { get; set; }
    public string? SerializedException { get; set; }
    public string? SerializedActivityStateCompressionAlgorithm { get; set; }
}