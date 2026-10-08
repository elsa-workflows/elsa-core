using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;

namespace Elsa.Workflows;

/// <summary>
/// Provides utilities that assigns unique identities to activity graph nodes. 
/// </summary>
public interface IIdentityGraphService
{
    /// <summary>
    /// Assign unique identities tot the specified <see cref="Workflow"/> and its activities.
    /// </summary>
    Task AssignIdentitiesAsync(Workflow workflow, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Assign unique identities tot the specified <see cref="IActivity"/> graph.
    /// </summary>
    Task AssignIdentitiesAsync(IActivity root, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Assign unique identities tot the specified <see cref="ActivityNode"/> graph.
    /// </summary>
    Task AssignIdentitiesAsync(ActivityNode root);
    
    /// <summary>
    /// Assign unique identities tot the specified flattened list of <see cref="ActivityNode"/>s.
    /// </summary>
    Task AssignIdentitiesAsync(ICollection<ActivityNode> flattenedList);    
    
    /// <summary>
    /// Assign unique identities to input and output properties of the specified <see cref="IActivity"/> graph.
    /// </summary>
    Task AssignInputOutputsAsync(IActivity activity);
    
    /// <summary>
    /// Assign unique identities to variables of the specified <see cref="IVariableContainer"/>.
    /// </summary>
    void AssignVariables(IVariableContainer activity);
}