using Elsa.Workflows.Models;

namespace Elsa.Workflows.Contracts;

/// <summary>
/// Walks an activity tree starting at the root.
/// </summary>
public interface IActivityVisitor
{
    /// <summary>
    /// Visits the specified activity and returns a tree structure representing the activity and its descendants.
    /// </summary>
    /// <param name="activity">The activity to visit.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A tree structure representing the activity and its descendants.</returns>
    Task<ActivityNode> VisitAsync(IActivity activity, CancellationToken cancellationToken = default);
    
}