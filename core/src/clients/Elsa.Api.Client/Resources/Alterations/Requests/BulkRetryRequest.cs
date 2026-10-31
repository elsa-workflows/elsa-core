namespace Elsa.Api.Client.Resources.Alterations.Requests;

/// <summary>
/// Represents a request to bulk retry workflow instances.
/// </summary>
public class BulkRetryRequest
{
    /// <summary>
    /// The IDs of the workflow instances that have incidents to be retried.
    /// </summary>
    public ICollection<string> WorkflowInstanceIds { get; set; } = new List<string>();

    /// <summary>
    /// An optional list of explicitly specified activity IDs to retry. If omitted, all faulted activities will be retried.
    /// </summary>
    public ICollection<string>? ActivityIds { get; set; }
}