using System.Text.Json.Nodes;

namespace Elsa.Studio.Workflows.Designer.Models;

/// <summary>
/// Represents the Studio graph model for a StateMachine activity.
/// </summary>
public class StateMachineGraph
{
    /// <summary>
    /// Gets or sets the source activity JSON.
    /// </summary>
    public JsonObject Activity { get; set; } = [];

    /// <summary>
    /// Gets or sets the initial state name.
    /// </summary>
    public string? InitialState { get; set; }

    /// <summary>
    /// Gets or sets the current state name.
    /// </summary>
    public string? CurrentState { get; set; }

    /// <summary>
    /// Gets or sets the state nodes.
    /// </summary>
    public ICollection<StateMachineStateNode> States { get; set; } = new List<StateMachineStateNode>();

    /// <summary>
    /// Gets or sets the transition edges.
    /// </summary>
    public ICollection<StateMachineTransitionEdge> Transitions { get; set; } = new List<StateMachineTransitionEdge>();

    /// <summary>
    /// Gets or sets validation issues discovered while mapping or validating the graph.
    /// </summary>
    public ICollection<StateMachineValidationIssue> ValidationIssues { get; set; } = new List<StateMachineValidationIssue>();
}
