namespace Elsa.Workflows.Runtime.Models;

public record DispatchWorkflowInstanceRequest(
    string InstanceId, 
    string? BookmarkId = default,
    string? ActivityId = default,
    string? ActivityNodeId = default,
    string? ActivityInstanceId = default,
    string? ActivityHash = default,
    IDictionary<string, object>? Input = default, 
    string? CorrelationId = default);