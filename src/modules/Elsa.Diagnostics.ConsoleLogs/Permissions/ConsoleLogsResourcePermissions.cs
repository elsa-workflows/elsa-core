using Elsa.Authorization;
using Elsa.Permissions;
using JetBrains.Annotations;

namespace Elsa.Diagnostics.ConsoleLogs.Permissions;

/// <summary>
/// Stable resource names for Diagnostics. Endpoints reference these constants rather than string
/// literals, and the descriptors below are declared alongside them so the two cannot drift.
/// </summary>
public static class ConsoleLogsResourcePermissions
{
    /// <summary>Read live and recent console logs.</summary>
    public const string ConsoleLogs = "diagnostics/console-logs";
}

/// <summary>Contributes the Diagnostics resources to the permission catalog.</summary>
[UsedImplicitly]
public sealed class ConsoleLogsResourcePermissionsDescriptorProvider : IPermissionDescriptorProvider
{
    /// <inheritdoc />
    public IEnumerable<PermissionDescriptor> GetDescriptors() =>
    [
        new(ConsoleLogsResourcePermissions.ConsoleLogs, [CoreVerbs.View], "Console logs", "Read live and recent console logs.", "Diagnostics"),
    ];
}
