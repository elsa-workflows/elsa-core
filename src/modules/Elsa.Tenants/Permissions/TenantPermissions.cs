using Elsa.Authorization;
using Elsa.Permissions;
using JetBrains.Annotations;

namespace Elsa.Tenants.Permissions;

/// <summary>
/// Stable resource names for Tenants. Endpoints reference these constants rather than string
/// literals, and the descriptors below are declared alongside them so the two cannot drift.
/// </summary>
public static class TenantPermissions
{
    /// <summary>Manage tenants and refresh the tenant registry.</summary>
    public const string Tenants = "tenants";
}

/// <summary>Contributes the Tenants resources to the permission catalog.</summary>
[UsedImplicitly]
public sealed class TenantPermissionsDescriptorProvider : IPermissionDescriptorProvider
{
    /// <inheritdoc />
    public IEnumerable<PermissionDescriptor> GetDescriptors() =>
    [
        new(TenantPermissions.Tenants, [CoreVerbs.View, CoreVerbs.Write, CoreVerbs.Delete, "refresh"], "Tenants", "Manage tenants and refresh the tenant registry.", "Tenants"),
    ];
}
