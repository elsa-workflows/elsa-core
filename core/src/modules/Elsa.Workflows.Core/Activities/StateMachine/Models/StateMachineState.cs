using System.ComponentModel;
using Elsa.Workflows.Attributes;

namespace Elsa.Workflows.Activities.StateMachine.Models;

/// <summary>
/// Represents a named state in a state machine.
/// </summary>
public class StateMachineState
{
    /// <summary>
    /// The state name.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// The activity to execute when entering this state.
    /// </summary>
    [Port]
    [Browsable(false)]
    public IActivity? Entry { get; set; }

    /// <summary>
    /// The activity to execute when leaving this state.
    /// </summary>
    [Port]
    [Browsable(false)]
    public IActivity? Exit { get; set; }
}
