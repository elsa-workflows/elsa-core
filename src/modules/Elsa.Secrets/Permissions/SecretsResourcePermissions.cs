using Elsa.Authorization;
using Elsa.Permissions;
using JetBrains.Annotations;

namespace Elsa.Secrets.Permissions;

/// <summary>
/// Stable resource names for Secrets. Endpoints reference these constants rather than string
/// literals, and the descriptors below are declared alongside them so the two cannot drift.
/// </summary>
public static class SecretsResourcePermissions
{
    /// <summary>Manage secret records, including rotation and revocation.</summary>
    public const string Secrets = "secrets";
}

/// <summary>Contributes the Secrets resources to the permission catalog.</summary>
[UsedImplicitly]
public sealed class SecretsResourcePermissionsDescriptorProvider : IPermissionDescriptorProvider
{
    /// <inheritdoc />
    public IEnumerable<PermissionDescriptor> GetDescriptors() =>
    [
        new(SecretsResourcePermissions.Secrets, [CoreVerbs.View, CoreVerbs.Write, CoreVerbs.Delete, "test"], "Secrets", "Manage secret records, including rotation and revocation.", "Secrets"),
    ];
}
