using Elsa.Workflows.Models;

namespace Elsa.MassTransit.Messages;

public class DispatchWorkflowInstance(string instanceId)
{
    public string InstanceId { get; init; } = instanceId;
    public string? BookmarkId { get; set; }
    public ActivityHandle? ActivityHandle { get; set; }
    public IDictionary<string, object>? Input { get; set; }

    public string? SerializedInput { get; set; }
    public IDictionary<string, object>? Properties { get; set; }
    public string? CorrelationId { get; set; }
}