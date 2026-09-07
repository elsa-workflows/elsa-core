using Elsa.Authorization;
using Elsa.Permissions;
using JetBrains.Annotations;

namespace Elsa.Diagnostics.StructuredLogs.Permissions;

/// <summary>
/// Stable resource names for Diagnostics. Endpoints reference these constants rather than string
/// literals, and the descriptors below are declared alongside them so the two cannot drift.
/// </summary>
public static class StructuredLogsResourcePermissions
{
    /// <summary>Read structured log records and their sources.</summary>
    public const string StructuredLogs = "diagnostics/structured-logs";
}

/// <summary>Contributes the Diagnostics resources to the permission catalog.</summary>
[UsedImplicitly]
public sealed class StructuredLogsResourcePermissionsDescriptorProvider : IPermissionDescriptorProvider
{
    /// <inheritdoc />
    public IEnumerable<PermissionDescriptor> GetDescriptors() =>
    [
        new(StructuredLogsResourcePermissions.StructuredLogs, [CoreVerbs.View], "Structured logs", "Read structured log records and their sources.", "Diagnostics"),
    ];
}
