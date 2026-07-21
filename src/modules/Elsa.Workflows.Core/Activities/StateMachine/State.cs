using System.Text.Json.Serialization;
using Elsa.Workflows.Activities;

namespace Elsa.Workflows.Activities.StateMachine;

/// <summary>
/// Represents a state within a <see cref="StateMachine"/> activity.
/// </summary>
public class State
{
    /// <summary>
    /// The name of the state.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Activity to execute when entering the state.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IActivity? Entry { get; set; }

    /// <summary>
    /// Activity to execute when exiting the state.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IActivity? Exit { get; set; }

    /// <summary>
    /// Transitions leaving this state.
    /// </summary>
    public ICollection<Transition> Transitions { get; set; } = new List<Transition>();
}
