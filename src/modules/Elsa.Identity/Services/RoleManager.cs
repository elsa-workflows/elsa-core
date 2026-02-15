using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Humanizer;

namespace Elsa.Identity.Services;

/// <summary>
/// Default implementation of <see cref="IRoleManager"/>.
/// </summary>
public class RoleManager : IRoleManager
{
    private readonly IRoleStore _roleStore;

    public RoleManager(IRoleStore roleStore)
    {
        _roleStore = roleStore;
    }

    /// <inheritdoc />
    public async Task<CreateRoleResult> CreateRoleAsync(
        string name,
        ICollection<string>? permissions = null,
        string? id = null,
        CancellationToken cancellationToken = default)
    {
        var roleId = id ?? name.Kebaberize();

        var role = new Role
        {
            Id = roleId,
            Name = name,
            Permissions = permissions ?? new List<string>()
        };

        await _roleStore.SaveAsync(role, cancellationToken);

        return new CreateRoleResult(role);
    }
}
