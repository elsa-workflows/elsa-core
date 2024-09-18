namespace Elsa.MassTransit.Messages;

public class DispatchWorkflowInstance(string instanceId)
{
    public string InstanceId { get; init; } = instanceId;
    public string? BookmarkId { get; set; }
    public string? ActivityId { get; set; }
    public string? ActivityNodeId { get; set; }
    public string? ActivityInstanceId { get; set; }
    public string? ActivityHash { get; set; }

    [Obsolete("This property is no longer used and will be removed in a future version. Use the SerializedInput property instead.")]
    public IDictionary<string, object>? Input { get; set; }

    public string? SerializedInput { get; set; }
    public IDictionary<string, object>? Properties { get; set; }
    public string? CorrelationId { get; set; }
}