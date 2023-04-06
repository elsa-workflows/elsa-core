namespace Elsa.MassTransit.Messages;

public record DispatchWorkflowInstance
(
    string InstanceId,
    string? BookmarkId,
    string? ActivityId,
    string? ActivityNodeId,
    string? ActivityInstanceId,
    string? ActivityHash,
    IDictionary<string, object>? Input,
    string? CorrelationId
);