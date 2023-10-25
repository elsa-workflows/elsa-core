namespace Elsa.Workflows.Core.Contracts;

/// <summary>
/// An activity resolver inspects a given activity and returns its contained activities.
/// They are used to inspect a workflow structure and build a graph of nodes from it for easy node traversal.
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
    bool GetSupportsActivity(IActivity activity);
    /// <summary>
    /// Returns a list of contained activities for the specified activity.
    /// </summary>
    ValueTask<IEnumerable<IActivity>> GetActivitiesAsync(IActivity activity, CancellationToken cancellationToken = default);
}