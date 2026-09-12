using Elsa.Authorization;
using Elsa.Permissions;
using JetBrains.Annotations;

namespace Elsa.Diagnostics.OpenTelemetry.Permissions;

/// <summary>
/// Stable resource names for Diagnostics. Endpoints reference these constants rather than string
/// literals, and the descriptors below are declared alongside them so the two cannot drift.
/// </summary>
public static class OpenTelemetryResourcePermissions
{
    /// <summary>Search traces, logs, metrics, and resources.</summary>
    public const string OpenTelemetry = "diagnostics/opentelemetry";
}

/// <summary>Contributes the Diagnostics resources to the permission catalog.</summary>
[UsedImplicitly]
public sealed class OpenTelemetryResourcePermissionsDescriptorProvider : IPermissionDescriptorProvider
{
    /// <inheritdoc />
    public IEnumerable<PermissionDescriptor> GetDescriptors() =>
    [
        new(OpenTelemetryResourcePermissions.OpenTelemetry, [CoreVerbs.View], "OpenTelemetry", "Search traces, logs, metrics, and resources.", "Diagnostics"),
    ];
}
