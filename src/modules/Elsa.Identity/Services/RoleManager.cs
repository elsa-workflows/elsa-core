using Elsa.Common.Multitenancy;
using Elsa.Extensions;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.Workflows;

namespace Elsa.Identity.Services;

/// <summary>
/// Default implementation of <see cref="IRoleManager"/>.
/// </summary>
/// <remarks>
/// Role IDs are unique across the whole store, which tenants may share, while role names are unique only per
/// tenant. A new role therefore gets a generated ID rather than one derived from its name, and a name is checked
/// for duplicates within the current tenant only, so two tenants can each hold a role with the same name.
/// </remarks>
public class RoleManager : IRoleManager
{
    private readonly IRoleStore _roleStore;
    private readonly IRoleProvider _roleProvider;
    private readonly ITenantAccessor _tenantAccessor;
    private readonly IIdentityGenerator _identityGenerator;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoleManager"/> class.
    /// </summary>
    public RoleManager(IRoleStore roleStore, IRoleProvider roleProvider, ITenantAccessor tenantAccessor, IIdentityGenerator identityGenerator)
    {
        _roleStore = roleStore;
        _roleProvider = roleProvider;
        _tenantAccessor = tenantAccessor;
        _identityGenerator = identityGenerator;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RoleManager"/> class that generates role IDs with
    /// <see cref="GuidIdentityGenerator"/>.
    /// </summary>
    [Obsolete("Use the constructor that takes an IIdentityGenerator, so role IDs come from the host's configured generator.")]
    public RoleManager(IRoleStore roleStore, IRoleProvider roleProvider, ITenantAccessor tenantAccessor)
        : this(roleStore, roleProvider, tenantAccessor, new GuidIdentityGenerator())
    {
    }

    /// <inheritdoc />
    public async Task<CreateRoleResult> CreateRoleAsync(
        string name,
        ICollection<string>? permissions = null,
        string? id = null,
        CancellationToken cancellationToken = default)
    {
        // Trimmed so the duplicate check below and the store's per-tenant name index agree: SQL Server, for one,
        // ignores trailing spaces when comparing, which would otherwise turn a near-duplicate into a 500.
        name = name.Trim();

        if (await FindSameNamedRoleAsync(name, cancellationToken) is { } sameNamedRole)
        {
            throw new InvalidOperationException($"A role named '{sameNamedRole.Name}' already exists.");
        }

        var roleId = string.IsNullOrWhiteSpace(id) ? _identityGenerator.GenerateId() : id;

        if (await RoleExistsAsync(roleId, cancellationToken))
        {
            throw new InvalidOperationException($"A role with ID '{roleId}' already exists.");
        }

        var role = new Role
        {
            Id = roleId,
            Name = name,
            // The in-memory path does not run EF's ApplyTenantId saving handler.
            TenantId = _tenantAccessor.TenantId,
            Permissions = permissions ?? new List<string>()
        };

        await _roleStore.SaveAsync(role, cancellationToken);

        return new CreateRoleResult(role);
    }

    /// <summary>
    /// Finds a role owned by the current tenant whose name differs from <paramref name="name"/> at most in case.
    /// Names that differ only in case used to collide on their derived ID, and case-insensitive databases reject
    /// them through the per-tenant name index, so they are rejected here on every store alike. Tenant-agnostic
    /// roles are visible to the tenant but owned by none, and the stores' per-tenant name uniqueness lets a tenant
    /// hold its own role of the same name, so they do not count.
    /// </summary>
    private async Task<Role?> FindSameNamedRoleAsync(string name, CancellationToken cancellationToken)
    {
        var tenantRoles = await _roleStore.FindManyAsync(new RoleFilter(), cancellationToken);
        return tenantRoles
            .Where(x => !RoleStoreExtensions.IsTenantAgnostic(x))
            .FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
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
