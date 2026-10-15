using Elsa.Authorization;
using Elsa.Permissions;
using JetBrains.Annotations;

namespace Elsa.Shells.Api.Permissions;

/// <summary>
/// Stable resource names for Platform. Endpoints reference these constants rather than string
/// literals, and the descriptors below are declared alongside them so the two cannot drift.
/// </summary>
public static class ShellPermissions
{
    /// <summary>Reload application shells.</summary>
    public const string Shells = "system/shells";
}

/// <summary>Contributes the Platform resources to the permission catalog.</summary>
[UsedImplicitly]
public sealed class ShellPermissionsDescriptorProvider : IPermissionDescriptorProvider
{
    /// <inheritdoc />
    public IEnumerable<PermissionDescriptor> GetDescriptors() =>
    [
        new(ShellPermissions.Shells, ["reload"], "Shells", "Reload application shells.", "Platform"),
    ];
}
