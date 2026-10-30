using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extensions for <see cref="IRoleProvider"/>.
/// </summary>
public static class RoleProviderExtensions
{
    /// <summary>
    /// Finds the roles with the specified IDs.
    /// </summary>
    /// <param name="roleProvider">The role provider.</param>
    /// <param name="ids">The IDs to filter for.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The roles with the specified IDs.</returns>
    public static async Task<IEnumerable<Role>> FindByIdsAsync(this IRoleProvider roleProvider, IEnumerable<string> ids, CancellationToken cancellationToken = default)
    {
        var filter = new RoleFilter()
        {
            Ids = ids.Distinct().ToList()
        };
        
        return await roleProvider.FindManyAsync(filter, cancellationToken);
    }
}