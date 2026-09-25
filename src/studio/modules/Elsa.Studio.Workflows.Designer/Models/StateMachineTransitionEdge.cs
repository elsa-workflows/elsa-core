using System.Text.Json.Nodes;

namespace Elsa.Studio.Workflows.Designer.Models;

/// <summary>
/// Represents one transition edge in a StateMachine designer graph.
/// </summary>
public class StateMachineTransitionEdge
{
    /// <summary>
    /// Gets or sets the optional transition name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the optional display name.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the source state name.
    /// </summary>
    public string From { get; set; } = "";

    /// <summary>
    /// Gets or sets the target state name.
    /// </summary>
    public string To { get; set; } = "";

    /// <summary>
    /// Gets or sets the optional trigger activity slot.
    /// </summary>
    public JsonNode? Trigger { get; set; }

    /// <summary>
    /// Gets or sets the optional condition input.
    /// </summary>
    public JsonNode? Condition { get; set; }

    /// <summary>
    /// Gets or sets the optional action activity slot.
    /// </summary>
    public JsonNode? Action { get; set; }

    /// <summary>
    /// Gets or sets the source transition JSON, including any unknown properties to preserve.
    /// </summary>
    public JsonObject Source { get; set; } = [];
}
