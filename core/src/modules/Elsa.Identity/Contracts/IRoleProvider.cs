using Elsa.Identity.Entities;
using Elsa.Identity.Models;

namespace Elsa.Identity.Contracts;

/// <summary>
/// Represents a provider of roles.
/// </summary>
public interface IRoleProvider
{
    /// <summary>
    /// Finds all roles matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter to use.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The roles matching the specified filter.</returns>
    ValueTask<IEnumerable<Role>> FindManyAsync(RoleFilter filter, CancellationToken cancellationToken = default);
}