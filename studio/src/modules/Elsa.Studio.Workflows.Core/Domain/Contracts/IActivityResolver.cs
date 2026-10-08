using System.Text.Json.Nodes;
using Elsa.Studio.Workflows.Domain.Models;

namespace Elsa.Studio.Workflows.Domain.Contracts;

/// <summary>
/// An activity resolver inspects a given activity and returns its contained activities.
/// Node resolvers are used to inspect a workflow structure and build a graph of nodes from it for easy node traversal.
/// </summary>
public interface IActivityResolver
{
    /// <summary>
    /// The priority of this resolver. Resolvers with higher priority are executed first.
    /// </summary>
    int Priority { get; }
    
    /// <summary>
    /// Returns true if this resolver supports the specified activity.
    /// </summary>
    bool GetSupportsActivity(JsonObject activity);
    
    /// <summary>
    /// Returns a list of contained activities for the specified activity.
    /// </summary>
    /// <param name="activity"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    ValueTask<IEnumerable<EmbeddedActivity>> GetActivitiesAsync(JsonObject activity, CancellationToken cancellationToken = default);
}