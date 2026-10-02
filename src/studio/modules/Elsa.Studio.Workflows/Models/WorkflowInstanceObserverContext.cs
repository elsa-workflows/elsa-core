using System.Text.Json.Nodes;

namespace Elsa.Studio.Workflows.Models;

/// <summary>
/// Represents the workflow instance observer context.
/// </summary>
public class WorkflowInstanceObserverContext
{
    /// <summary>
    /// Gets or sets the workflow instance id.
    /// </summary>
    public string WorkflowInstanceId { get; set; } = default!;
    /// <summary>
    /// Gets or sets the container activity.
    /// </summary>
    public JsonObject? ContainerActivity { get; set; }
    /// <summary>
    /// Gets or sets the cancellation token.
    /// </summary>
    public CancellationToken CancellationToken { get; set; }
}