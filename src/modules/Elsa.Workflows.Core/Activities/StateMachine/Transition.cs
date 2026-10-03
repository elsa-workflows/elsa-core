using System.Text.Json.Serialization;
using Elsa.Expressions.Models;
using Elsa.Workflows.Activities;

namespace Elsa.Workflows.Activities.StateMachine;

/// <summary>
/// Represents a transition between states.
/// </summary>
public class Transition
{
    /// <summary>
    /// The name of the transition.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The source state.
    /// </summary>
    public string From { get; set; } = null!;

    /// <summary>
    /// The target state.
    /// </summary>
    public string To { get; set; } = null!;

    /// <summary>
    /// The trigger activity that causes this transition.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IActivity? Trigger { get; set; }

    /// <summary>
    /// Optional condition that must evaluate to true for the transition to occur.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Input<bool>? Condition { get; set; }

    /// <summary>
    /// Activity to execute as part of the transition.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IActivity? Action { get; set; }
}
