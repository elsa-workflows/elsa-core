namespace Elsa.Bpmn.Interchange.Permissions;

/// <summary>
/// BPMN interchange operates on workflow definitions rather than on a resource of its own: analyze and
/// export read them, import writes them. It therefore reuses the workflow-definitions resource, matching
/// the permissions these endpoints required before the vocabulary change.
/// </summary>
/// <remarks>
/// The path is repeated here rather than referenced from Elsa.Workflows.Api, which this module does not
/// depend on and should not take a dependency on for one constant. No descriptor is contributed: the
/// resource is owned and described by Elsa.Workflows.Api, and the registry keeps a single entry per
/// resource.
/// </remarks>
internal static class BpmnPermissions
{
    /// <summary>The workflow-definitions resource, owned by Elsa.Workflows.Api.</summary>
    public const string Definitions = "workflows/definitions";
}
