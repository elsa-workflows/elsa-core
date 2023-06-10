namespace Elsa.Api.Client.Resources.WorkflowDefinitions.Models;

/// <summary>
/// Represents a container activity.
/// </summary>
public class Container : Activity
{
    /// <summary>
    /// Gets or sets the activities contained in this container.
    /// </summary>
    public ICollection<Activity> Activities { get; set; } = new List<Activity>();
    
    /// <summary>
    /// Gets or sets the variables in this container.
    /// </summary>
    public ICollection<Variable> Variables { get; set; } = new List<Variable>();
}