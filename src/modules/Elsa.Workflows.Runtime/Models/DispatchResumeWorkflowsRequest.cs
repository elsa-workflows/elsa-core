namespace Elsa.Workflows.Runtime.Models;

public record DispatchResumeWorkflowsRequest(string ActivityTypeName, object BookmarkPayload, string? CorrelationId = default, IDictionary<string, object>? Input = default);