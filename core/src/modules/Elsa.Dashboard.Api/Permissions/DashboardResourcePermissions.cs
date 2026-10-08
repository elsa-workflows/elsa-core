using Elsa.Authorization;
using Elsa.Permissions;
using JetBrains.Annotations;

namespace Elsa.Dashboard.Api.Permissions;

/// <summary>
/// Stable resource names for Dashboard. Endpoints reference these constants rather than string
/// literals, and the descriptors below are declared alongside them so the two cannot drift.
/// </summary>
public static class DashboardResourcePermissions
{
    /// <summary>
    /// View the whole operational dashboard. The permission guarding a section's data reads just that section.
    /// </summary>
    public const string Dashboard = "dashboard";
}

/// <summary>Contributes the Dashboard resources to the permission catalog.</summary>
[UsedImplicitly]
public sealed class DashboardResourcePermissionsDescriptorProvider : IPermissionDescriptorProvider
{
    /// <inheritdoc />
    public IEnumerable<PermissionDescriptor> GetDescriptors() =>
    [
        new(DashboardResourcePermissions.Dashboard, [CoreVerbs.View], "Dashboard", "View the whole operational dashboard. The permission that guards a section's data, such as workflows/instances:view, reads just that section.", "Dashboard"),
    ];
}
