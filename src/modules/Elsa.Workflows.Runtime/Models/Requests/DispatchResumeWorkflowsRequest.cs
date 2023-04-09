namespace Elsa.Workflows.Runtime.Models.Requests;

public record DispatchResumeWorkflowsRequest(
    string ActivityTypeName, 
    object BookmarkPayload, 
    string? CorrelationId = default,
    string? WorkflowInstanceId = default,
    IDictionary<string, object>? Input = default);