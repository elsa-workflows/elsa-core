using Elsa.Authorization;
using Elsa.Permissions;
using JetBrains.Annotations;

namespace Elsa.Identity.Permissions;

/// <summary>
/// Stable resource names for Identity. Endpoints reference these constants rather than string
/// literals, and the descriptors below are declared alongside them so the two cannot drift.
/// </summary>
public static class IdentityPermissions
{
    /// <summary>Manage user accounts.</summary>
    public const string Users = "identity/users";
    /// <summary>Manage roles and the permissions they carry.</summary>
    public const string Roles = "identity/roles";
    /// <summary>Create API client applications.</summary>
    public const string Applications = "identity/applications";
}

/// <summary>Contributes the Identity resources to the permission catalog.</summary>
[UsedImplicitly]
public sealed class IdentityPermissionsDescriptorProvider : IPermissionDescriptorProvider
{
    /// <inheritdoc />
    public IEnumerable<PermissionDescriptor> GetDescriptors() =>
    [
        new(IdentityPermissions.Users, [CoreVerbs.View, CoreVerbs.Create, CoreVerbs.Update, CoreVerbs.Delete], "Users", "Manage user accounts.", "Identity"),
        new(IdentityPermissions.Roles, [CoreVerbs.View, CoreVerbs.Create, CoreVerbs.Update, CoreVerbs.Delete], "Roles", "Manage roles and the permissions they carry.", "Identity"),
        new(IdentityPermissions.Applications, [CoreVerbs.Create], "Applications", "Create API client applications.", "Identity"),
    ];
}
