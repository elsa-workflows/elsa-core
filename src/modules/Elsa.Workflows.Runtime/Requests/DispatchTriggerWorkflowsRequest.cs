namespace Elsa.Workflows.Runtime.Requests;

/// <summary>
/// Represents a dispatch request to trigger all workflows using the provided information.
/// </summary>
/// <param name="activityTypeName">The type name of the activity to trigger.</param>
/// <param name="bookmarkPayload">AnyAsync bookmark payload to use to find the workflows to trigger.</param>
public class DispatchTriggerWorkflowsRequest(string activityTypeName, object bookmarkPayload)
{
    /// <summary>The type name of the activity to trigger.</summary>
    public string ActivityTypeName { get; init; } = activityTypeName;

    /// <summary>AnyAsync bookmark payload to use to find the workflows to trigger.</summary>
    public object BookmarkPayload { get; init; } = bookmarkPayload;

    /// <summary>AnyAsync correlation ID to use to find the workflows to trigger.</summary>
    public string? CorrelationId { get; set; }

    public string? WorkflowInstanceId { get; set; }
    public string? ActivityInstanceId { get; set; }

    /// <summary>Any input to send along.</summary>
    public IDictionary<string, object>? Input { get; set; }
    
    /// <summary>
    /// Any properties to attach to the workflow instance.
    /// </summary>
    public IDictionary<string, object>? Properties { get; set; }
}