using Elsa.Identity.Models;

namespace Elsa.Identity.Contracts;

/// <summary>
/// Allows an installed module to guard role deletion and remove its editable role references.
/// </summary>
public interface IRoleDeletionDependencyContributor
{
    /// <summary>A stable contributor identifier.</summary>
    string Source { get; }

    /// <summary>Returns all references to the specified role, including immutable references.</summary>
    ValueTask<RoleDeletionDependencySnapshot> InspectAsync(string roleId, CancellationToken cancellationToken = default);

    /// <summary>Validates authorization and optimistic-concurrency inputs without mutating state.</summary>
    ValueTask<RoleReferenceRemovalValidationResult> ValidateRemovalAsync(RoleReferenceRemovalRequest request, CancellationToken cancellationToken = default);

    /// <summary>Removes the prevalidated editable references.</summary>
    ValueTask<RoleReferenceRemovalResult> RemoveEditableReferencesAsync(RoleReferenceRemovalRequest request, CancellationToken cancellationToken = default);
}
