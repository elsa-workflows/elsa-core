using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extensions for <see cref="IUserProvider"/>.
/// </summary>
public static class UserProviderExtensions
{
    /// <summary>
    /// Finds the user with the specified name.
    /// </summary>
    /// <param name="userProvider">The user provider.</param>
    /// <param name="name">The name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The user with the specified name.</returns>
    public static Task<User?> FindByNameAsync(this IUserProvider userProvider, string name, CancellationToken cancellationToken = default)
    {
        return userProvider.FindAsync(new UserFilter
        {
            Name = name
        }, cancellationToken);
    }

    /// <summary>
    /// Finds the user with the specified identifier.
    /// </summary>
    /// <param name="userProvider">The user provider.</param>
    /// <param name="id">The identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The user with the specified identifier.</returns>
    /// <remarks>
    /// Identity refresh looks users up by this id and relies on ids being globally unique.
    /// </remarks>
    public static Task<User?> FindByIdAsync(this IUserProvider userProvider, string id, CancellationToken cancellationToken = default)
    {
        return userProvider.FindAsync(new UserFilter
        {
            Id = id
        }, cancellationToken);
    }
}