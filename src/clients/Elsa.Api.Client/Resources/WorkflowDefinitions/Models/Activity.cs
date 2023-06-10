namespace Elsa.Api.Client.Resources.WorkflowDefinitions.Models;

/// <summary>
/// Represents an activity in a workflow definition.
/// </summary>
public class Activity : Dictionary<string, object>
{
    /// <summary>
    /// Gets or sets the ID of this activity.
    /// </summary>
    public string Id { get; set; } = default!;
    
    /// <summary>
    /// Gets or sets the type of this activity.
    /// </summary>
    public string Type { get; set; } = default!;
    
    /// <summary>
    /// Gets or sets the version of this activity.
    /// </summary>
    public int Version { get; set; }
    
    /// <summary>
    /// Gets or sets the metadata of this activity.
    /// </summary>
    public IDictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    
    /// <summary>
    /// Gets or sets whether this activity can be used as a trigger to start a workflow.
    /// </summary>
    public bool CanStartWorkflow { get; set; }
    
    /// <summary>
    /// Gets or sets a value whether this activity should execute asynchronously.
    /// </summary>
    public bool RunAsynchronously { get; set; }

    /// <summary>
    /// Gets or sets custom properties of this activity.
    /// </summary>
    public IDictionary<string, object> CustomProperties { get; set; } = new Dictionary<string, object>();

}