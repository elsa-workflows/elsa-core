namespace Elsa.MassTransit.Messages;

// ReSharper disable once InconsistentNaming
public interface DispatchWorkflowInstance
{
    string InstanceId { get; set; }
    string? BookmarkId { get; set; }
    string? ActivityId { get; set; }
    IDictionary<string, object>? Input { get; set; }
    string? CorrelationId { get; set; }
}