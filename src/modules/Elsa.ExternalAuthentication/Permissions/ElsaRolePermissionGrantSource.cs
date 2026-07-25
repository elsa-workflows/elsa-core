using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.Identity.Contracts;
using Elsa.Identity.Models;

namespace Elsa.ExternalAuthentication.Permissions;

public sealed class ElsaRolePermissionGrantSource(IUserProvider userProvider, IRoleProvider roleProvider) : IPermissionGrantSource
{
    public const string SourceType = "elsa-roles";
    public string Type => SourceType;

    public PermissionGrantSourceDescriptor Describe() => new(Type, "Elsa roles", "Grants the permissions assigned to the Elsa user's current roles.", 1, [], null);

    public async ValueTask<PermissionGrantResult> GetGrantsAsync(PermissionGrantContext context, CancellationToken cancellationToken = default)
    {
        var user = await userProvider.FindAsync(new UserFilter { Id = context.UserId }, cancellationToken);
        if (user is null || !string.Equals(user.TenantId, context.TargetTenantId, StringComparison.Ordinal))
            return new PermissionGrantResult([], [new PermissionGrantWarning("user_not_available", "The Elsa user is not available in the target tenant.")]);

        var roles = await roleProvider.FindManyAsync(new RoleFilter { Ids = user.Roles.ToArray() }, cancellationToken);
        var grants = roles
            .Where(x => string.Equals(x.TenantId, context.TargetTenantId, StringComparison.Ordinal))
            .OrderBy(x => x.Id, StringComparer.Ordinal)
            .SelectMany(role => role.Permissions.Where(permission => !string.IsNullOrWhiteSpace(permission)).OrderBy(permission => permission, StringComparer.Ordinal).Select(permission => new PermissionGrant(permission, Type, role.Id)))
            .ToArray();
        return new PermissionGrantResult(grants, []);
    }
}
