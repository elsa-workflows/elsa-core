using Elsa.Identity.Entities;
using Elsa.Identity.Models;

namespace Elsa.Identity.Contracts;

/// <summary>
/// Represents a user store.
/// </summary>
public interface IUserStore
{
    /// <summary>
    /// Saves the user.
    /// </summary>
    /// <param name="user">The user to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The task.</returns>
    Task SaveAsync(User user, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Removes the users matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The task.</returns>
    Task DeleteAsync(UserFilter filter, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Finds the user matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The user matching the specified filter.</returns>
    Task<User?> FindAsync(UserFilter filter, CancellationToken cancellationToken = default);
}