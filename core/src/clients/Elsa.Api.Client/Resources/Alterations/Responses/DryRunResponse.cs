namespace Elsa.Api.Client.Resources.Alterations.Responses;

/// <summary>
/// The response to the DryRun request
/// </summary>
public class DryRunResponse
{
    /// <summary>
    /// The list of workflow instance IDs that would be affected by a "Submit" request
    /// </summary>
    public ICollection<string> WorkflowInstanceIds { get; set; } = new List<string>();
}