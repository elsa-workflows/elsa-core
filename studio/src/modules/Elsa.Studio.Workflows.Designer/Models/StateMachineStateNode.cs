using System.Text.Json.Nodes;

namespace Elsa.Studio.Workflows.Designer.Models;

/// <summary>
/// Represents one state node in a StateMachine designer graph.
/// </summary>
public class StateMachineStateNode
{
    /// <summary>
    /// Gets or sets the state name.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Gets or sets the optional entry activity slot.
    /// </summary>
    public JsonNode? Entry { get; set; }

    /// <summary>
    /// Gets or sets the optional exit activity slot.
    /// </summary>
    public JsonNode? Exit { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this state has no valid outbound transitions.
    /// </summary>
    public bool IsTerminal { get; set; }

    /// <summary>
    /// Gets or sets the source state JSON, including any unknown properties to preserve.
    /// </summary>
    public JsonObject Source { get; set; } = [];
}
