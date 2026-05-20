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
    private readonly IRoleProvider _roleProvider;

    public RoleManager(IRoleStore roleStore, IRoleProvider roleProvider)
    {
        _roleStore = roleStore;
        _roleProvider = roleProvider;
    }

    /// <inheritdoc />
    public async Task<CreateRoleResult> CreateRoleAsync(
        string name,
        ICollection<string>? permissions = null,
        string? id = null,
        CancellationToken cancellationToken = default)
    {
        var roleId = id ?? name.Kebaberize();

        if (await RoleExistsAsync(roleId, cancellationToken))
            throw new InvalidOperationException($"A role with ID '{roleId}' already exists.");

        var role = new Role
        {
            Id = roleId,
            Name = name,
            Permissions = permissions ?? new List<string>()
        };

        await _roleStore.SaveAsync(role, cancellationToken);

        return new CreateRoleResult(role);
    }

    private async Task<bool> RoleExistsAsync(string roleId, CancellationToken cancellationToken)
    {
        var storedRole = await _roleStore.FindAsync(new() { Id = roleId }, cancellationToken);
        if (storedRole != null)
            return true;

        var providedRoles = await _roleProvider.FindManyAsync(new() { Id = roleId }, cancellationToken);
        return providedRoles.Any(x => x.Id == roleId);
    }
}
