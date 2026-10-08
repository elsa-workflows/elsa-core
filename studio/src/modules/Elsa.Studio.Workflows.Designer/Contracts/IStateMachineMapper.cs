using System.Text.Json.Nodes;
using Elsa.Studio.Workflows.Designer.Models;

namespace Elsa.Studio.Workflows.Designer.Contracts;

/// <summary>
/// Maps a StateMachine activity from and to a graph representation.
/// </summary>
public interface IStateMachineMapper
{
    /// <summary>
    /// Maps a StateMachine activity JSON object to a graph representation.
    /// </summary>
    StateMachineGraph Map(JsonObject activity);

    /// <summary>
    /// Maps a graph representation back to StateMachine activity JSON.
    /// </summary>
    JsonObject Map(StateMachineGraph graph);
}
