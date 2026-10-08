using Elsa.Identity.Entities;
using Elsa.Identity.Models;

namespace Elsa.Identity.Contracts;

/// <summary>Allows an installed module to prevent deletion of a user it still references.</summary>
public interface IUserDeletionDependencyContributor
{
    /// <summary>A stable contributor identifier.</summary>
    string Source { get; }

    /// <summary>Returns the dependency that prevents deleting the user, or <see langword="null"/> when none exists.</summary>
    ValueTask<UserDeletionDependency?> InspectAsync(User user, CancellationToken cancellationToken = default);
}
