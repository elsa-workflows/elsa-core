using Elsa.Authorization;
using Elsa.Permissions;
using JetBrains.Annotations;

namespace Elsa.Expressions.JavaScript.Permissions;

/// <summary>
/// Stable resource names for Workflows. Endpoints reference these constants rather than string
/// literals, and the descriptors below are declared alongside them so the two cannot drift.
/// </summary>
public static class JavaScriptPermissions
{
    /// <summary>Read JavaScript type definitions for the workflow editor.</summary>
    public const string Scripting = "workflows/scripting/javascript";
}

/// <summary>Contributes the Workflows resources to the permission catalog.</summary>
[UsedImplicitly]
public sealed class JavaScriptPermissionsDescriptorProvider : IPermissionDescriptorProvider
{
    /// <inheritdoc />
    public IEnumerable<PermissionDescriptor> GetDescriptors() =>
    [
        new(JavaScriptPermissions.Scripting, [CoreVerbs.View], "JavaScript type definitions", "Read JavaScript type definitions for the workflow editor.", "Workflows"),
    ];
}
