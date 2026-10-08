using System.ComponentModel;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.UIHints;

namespace Elsa.Workflows.Activities.StateMachine.Models;

/// <summary>
/// Represents a directed transition between two states.
/// </summary>
public class Transition
{
    /// <summary>
    /// The transition name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// A human-readable transition name.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// The source state name.
    /// </summary>
    public string From { get; set; } = "";

    /// <summary>
    /// The target state name.
    /// </summary>
    public string To { get; set; } = "";

    /// <summary>
    /// The trigger activity that starts this transition.
    /// </summary>
    [Port]
    [Browsable(false)]
    public IActivity? Trigger { get; set; }

    /// <summary>
    /// The condition that determines whether the transition can be taken.
    /// When this evaluates to <c>false</c>, the trigger is re-armed until another trigger wins or the condition later evaluates to <c>true</c>.
    /// </summary>
    [Input(UIHint = InputUIHints.SingleLine)]
    public Input<bool>? Condition { get; set; }

    /// <summary>
    /// The activity to execute after the transition is accepted.
    /// </summary>
    [Port]
    [Browsable(false)]
    public IActivity? Action { get; set; }
}
