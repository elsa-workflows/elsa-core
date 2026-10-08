using Elsa.Authorization;
using Elsa.Permissions;
using JetBrains.Annotations;

namespace Elsa.Labels.Permissions;

/// <summary>
/// Stable resource names for Labels. Endpoints reference these constants rather than string
/// literals, and the descriptors below are declared alongside them so the two cannot drift.
/// </summary>
public static class LabelPermissions
{
    /// <summary>Manage labels.</summary>
    public const string Labels = "labels";

    /// <summary>View and change the labels applied to a workflow definition.</summary>
    public const string WorkflowDefinitionLabels = "workflows/definitions/labels";
}

/// <summary>Contributes the Labels resources to the permission catalog.</summary>
[UsedImplicitly]
public sealed class LabelPermissionsDescriptorProvider : IPermissionDescriptorProvider
{
    /// <inheritdoc />
    public IEnumerable<PermissionDescriptor> GetDescriptors() =>
    [
        new(LabelPermissions.Labels, [CoreVerbs.View, CoreVerbs.Create, CoreVerbs.Update, CoreVerbs.Delete], "Labels", "Manage labels.", "Labels"),
        new(LabelPermissions.WorkflowDefinitionLabels, [CoreVerbs.View, CoreVerbs.Update], "Workflow definition labels", "View and change the labels applied to a workflow definition.", "Labels"),
    ];
}
