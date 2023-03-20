using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Contracts;

/// <summary>
/// Walks an activity tree starting at the root.
/// </summary>
public interface IActivityVisitor
{
    Task<ActivityNode> VisitAsync(IActivity activity, CancellationToken cancellationToken = default);
    
}