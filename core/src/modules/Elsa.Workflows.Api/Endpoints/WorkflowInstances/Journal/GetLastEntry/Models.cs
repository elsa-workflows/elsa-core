namespace Elsa.Workflows.Api.Endpoints.WorkflowInstances.Journal.GetLastEntry;

/// <summary>
/// A request to get the last entry for the specified workflow instance and activity ID.
/// </summary>
public class Request
{
    /// <summary>
    /// The ID of the workflow instance to get the last entry for.
    /// </summary>
    public string WorkflowInstanceId { get; set; } = default!;
    

    /// <summary>
    /// The ID of the activity to get the last entry for.
    /// </summary>
    public string ActivityId { get; set; } = default!;
}