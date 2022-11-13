using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Services;

/// <summary>
/// Walks an activity tree starting at the root.
/// </summary>
public interface IActivityWalker
{
    Task<ActivityNode> WalkAsync(IActivity activity, CancellationToken cancellationToken = default);
    
}