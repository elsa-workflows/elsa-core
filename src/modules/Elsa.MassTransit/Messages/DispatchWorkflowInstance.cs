namespace Elsa.MassTransit.Messages;

public record DispatchWorkflowInstance
(
    string InstanceId,
    string? BookmarkId,
    string? ActivityId,
    IDictionary<string, object>? Input,
    string? CorrelationId
);