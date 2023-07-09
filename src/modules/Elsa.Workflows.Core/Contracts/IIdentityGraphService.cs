using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Contracts;

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
    void AssignIdentities(ActivityNode root);
    
    /// <summary>
    /// Assign unique identities tot the specified flattened list of <see cref="ActivityNode"/>s.
    /// </summary>
    void AssignIdentities(ICollection<ActivityNode> flattenedList);    
    
    /// <summary>
    /// Assign unique identities to input and output properties of the specified <see cref="IActivity"/> graph.
    /// </summary>
    void AssignInputOutputs(IActivity activity);
    
    /// <summary>
    /// Assign unique identities to variables of the specified <see cref="IVariableContainer"/>.
    /// </summary>
    void AssignVariables(IVariableContainer activity);
}