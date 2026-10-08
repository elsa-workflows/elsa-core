namespace Elsa.Workflows.Runtime.Requests;

public record DispatchResumeWorkflowsRequest(
    string ActivityTypeName, 
    object BookmarkPayload, 
    string? CorrelationId = null,
    string? WorkflowInstanceId = null,
    string? ActivityInstanceId = null,
    IDictionary<string, object>? Input = null);