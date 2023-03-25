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
    public static async Task<User?> FindByNameAsync(this IUserProvider userProvider, string name, CancellationToken cancellationToken = default)
    {
        var filter = new UserFilter()
        {
            Name = name
        };
        
        return await userProvider.FindAsync(filter, cancellationToken);
    }
}