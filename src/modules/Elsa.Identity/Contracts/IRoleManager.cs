using Elsa.Identity.Entities;
using Elsa.Identity.Models;

namespace Elsa.Identity.Contracts;

/// <summary>
/// Manages role operations such as creation.
/// </summary>
public interface IRoleManager
{
    /// <summary>
    /// Creates a new role with the specified details.
    /// </summary>
    /// <param name="name">The role name.</param>
    /// <param name="permissions">The permissions to assign to the role. If null, defaults to an empty list.</param>
    /// <param name="id">The optional role ID. If null, will be generated from the name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the created role.</returns>
    Task<CreateRoleResult> CreateRoleAsync(string name, ICollection<string>? permissions = null, string? id = null, CancellationToken cancellationToken = default);
}
