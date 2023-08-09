namespace Elsa.Workflows.Runtime.Requests;

public record DispatchResumeWorkflowsRequest(
    string ActivityTypeName, 
    object BookmarkPayload, 
    string? CorrelationId = default,
    string? WorkflowInstanceId = default,
    string? ActivityInstanceId = default,
    IDictionary<string, object>? Input = default);