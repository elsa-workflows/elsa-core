namespace Elsa.Workflows.Runtime.Models;

/// <summary>
/// Represents a dispatch request to trigger all workflows using the provided information.
/// </summary>
/// <param name="ActivityTypeName">The type name of the activity to trigger.</param>
/// <param name="BookmarkPayload">Any bookmark payload to use to find the workflows to trigger.</param>
/// <param name="CorrelationId">Any correlation ID to use to find the workflows to trigger.</param>
/// <param name="Input">Any input to send along.</param>
public record DispatchTriggerWorkflowsRequest(string ActivityTypeName, object BookmarkPayload, string? CorrelationId = default, IDictionary<string, object>? Input = default);