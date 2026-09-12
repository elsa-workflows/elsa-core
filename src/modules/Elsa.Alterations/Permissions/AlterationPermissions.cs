using Elsa.Authorization;
using Elsa.Permissions;
using JetBrains.Annotations;

namespace Elsa.Alterations.Permissions;

/// <summary>
/// Stable resource names for Alterations. Endpoints reference these constants rather than string
/// literals, and the descriptors below are declared alongside them so the two cannot drift.
/// </summary>
public static class AlterationPermissions
{
    /// <summary>Inspect and run alteration plans.</summary>
    public const string Alterations = "alterations";
}

/// <summary>Contributes the Alterations resources to the permission catalog.</summary>
[UsedImplicitly]
public sealed class AlterationPermissionsDescriptorProvider : IPermissionDescriptorProvider
{
    /// <inheritdoc />
    public IEnumerable<PermissionDescriptor> GetDescriptors() =>
    [
        new(AlterationPermissions.Alterations, [CoreVerbs.View, CoreVerbs.Execute], "Alterations", "Inspect and run alteration plans.", "Alterations"),
    ];
}
