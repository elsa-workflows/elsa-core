using Elsa.Identity.Entities;
using Elsa.Identity.Models;

namespace Elsa.Identity.Contracts;

/// <summary>
/// Manages user operations such as creation and validation.
/// </summary>
public interface IUserManager
{
    /// <summary>
    /// Creates a new user with the specified details.
    /// </summary>
    /// <param name="name">The user name (typically email).</param>
    /// <param name="password">The user's password. If null or empty, a password will be generated.</param>
    /// <param name="roles">The roles to assign to the user. If null, defaults to an empty list.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the created user and the plain-text password.</returns>
    Task<CreateUserResult> CreateUserAsync(string name, string? password = null, ICollection<string>? roles = null, CancellationToken cancellationToken = default);
}
