using System.Text.Json.Nodes;
using Elsa.Studio.Workflows.Designer.Models;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.UI.Models;

namespace Elsa.Studio.Workflows.Designer.Contracts;

/// <summary>
/// Maps a Sequence activity from and to an ordered graph representation.
/// </summary>
public interface ISequenceMapper
{
    /// <summary>
    /// Maps a Sequence activity to a graph with order-derived edges.
    /// </summary>
    X6Graph Map(JsonObject sequence, IDictionary<string, ActivityStats>? activityStatsMap = null);

    /// <summary>
    /// Maps a graph back to a Sequence activity.
    /// </summary>
    JsonObject Map(JsonObject sequence, X6Graph graph);
}
