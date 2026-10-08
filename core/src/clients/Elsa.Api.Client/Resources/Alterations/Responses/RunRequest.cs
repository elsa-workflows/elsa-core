using System.Text.Json.Nodes;

namespace Elsa.Api.Client.Resources.Alterations.Responses;

/// <summary>
/// A plan that contains a list of alterations to be applied to a set of workflow instances.
/// </summary>
public class RunRequest
{
    /// <summary>
    /// The alterations to be applied.
    /// </summary>
    public ICollection<JsonObject> Alterations { get; set; } = new List<JsonObject>();
    
    /// <summary>
    /// The IDs of the workflow instances that this plan applies to.
    /// </summary>
    public ICollection<string> WorkflowInstanceIds { get; set; } = new List<string>();
}