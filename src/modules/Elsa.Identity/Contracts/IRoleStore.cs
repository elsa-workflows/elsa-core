using Elsa.Identity.Entities;
using Elsa.Identity.Models;

namespace Elsa.Identity.Contracts;

/// <summary>
/// Represents a role store.
/// </summary>
public interface IRoleStore
{
    /// <summary>
    /// Adds the role.
    /// </summary>
    /// <param name="role">The role to add.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The task.</returns>
    Task AddAsync(Role role, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Removes the role with the specified ID.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The task.</returns>
    Task RemoveAsync(RoleFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the role matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The role matching the specified filter.</returns>
    Task<Role?> FindAsync(RoleFilter filter, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Finds the roles matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The roles matching the specified filter.</returns>
    Task<IEnumerable<Role>> FindManyAsync(RoleFilter filter, CancellationToken cancellationToken = default);
}