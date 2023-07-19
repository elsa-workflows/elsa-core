namespace Elsa.Workflows.Runtime.Requests;

/// <summary>
/// Represents a dispatch request to trigger all workflows using the provided information.
/// </summary>
/// <param name="ActivityTypeName">The type name of the activity to trigger.</param>
/// <param name="BookmarkPayload">AnyAsync bookmark payload to use to find the workflows to trigger.</param>
/// <param name="CorrelationId">AnyAsync correlation ID to use to find the workflows to trigger.</param>
/// <param name="Input">AnyAsync input to send along.</param>
public record DispatchTriggerWorkflowsRequest(string ActivityTypeName, object BookmarkPayload, string? CorrelationId = default, string? WorkflowInstanceId = default, IDictionary<string, object>? Input = default);