using Elsa.Authorization;
using Elsa.Permissions;
using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Permissions;

/// <summary>
/// Stable resource names for Workflows. Endpoints reference these constants rather than string
/// literals, and the descriptors below are declared alongside them so the two cannot drift.
/// </summary>
public static class WorkflowPermissions
{
    /// <summary>Author, publish, run, and refresh workflow definitions.</summary>
    public const string Definitions = "workflows/definitions";
    /// <summary>Delete and revert individual definition versions.</summary>
    public const string DefinitionVersions = "workflows/definitions/versions";
    /// <summary>Inspect, import, delete, and cancel workflow instances.</summary>
    public const string Instances = "workflows/instances";
    /// <summary>Inspect activity execution records and summaries.</summary>
    public const string ActivityExecutions = "workflows/activity-executions";
    /// <summary>Inspect runtime status, and pause, resume, or drain the runtime.</summary>
    public const string Runtime = "workflows/runtime";
    /// <summary>Inspect, replay, and delete bookmark queue dead-letter items.</summary>
    public const string BookmarkQueueDeadLetters = "workflows/bookmark-queue/dead-letters";
    /// <summary>Trigger workflow events.</summary>
    public const string Events = "workflows/events";
    /// <summary>Complete external workflow tasks.</summary>
    public const string Tasks = "workflows/tasks";
    /// <summary>Execute activity tests.</summary>
    public const string Tests = "workflows/tests";
    /// <summary>
    /// Still guards requesting a registry refresh (<c>GET /descriptors/activities?refresh=true</c>) and resolving an activity
    /// property's options (<c>POST /descriptors/activities/{activityTypeName}/options/{propertyName}</c>). Reading the
    /// activity catalog no longer requires it.
    /// </summary>
    public const string DescriptorsActivities = "workflows/descriptors/activities";
    /// <summary>
    /// Formerly guarded browsing the expression types, which now requires only an authenticated caller. Kept so roles that
    /// already hold it still resolve rather than being reported as invalid at startup.
    /// </summary>
    public const string DescriptorsExpressions = "workflows/descriptors/expressions";
    /// <summary>
    /// Formerly guarded browsing the storage drivers, which now requires only an authenticated caller. Kept so roles that
    /// already hold it still resolve rather than being reported as invalid at startup.
    /// </summary>
    public const string DescriptorsStorageDrivers = "workflows/descriptors/storage-drivers";
    /// <summary>
    /// Formerly guarded browsing the variable types, which now requires only an authenticated caller. Kept so roles that
    /// already hold it still resolve rather than being reported as invalid at startup.
    /// </summary>
    public const string DescriptorsVariables = "workflows/descriptors/variables";
    /// <summary>
    /// Formerly guarded browsing the commit strategies, which now requires only an authenticated caller. Kept so roles that
    /// already hold it still resolve rather than being reported as invalid at startup.
    /// </summary>
    public const string DescriptorsCommitStrategies = "workflows/descriptors/commit-strategies";
    /// <summary>
    /// Formerly guarded browsing the incident strategies, which now requires only an authenticated caller. Kept so roles that
    /// already hold it still resolve rather than being reported as invalid at startup.
    /// </summary>
    public const string DescriptorsIncidentStrategies = "workflows/descriptors/incident-strategies";
    /// <summary>
    /// Formerly guarded browsing the log persistence strategies, which now requires only an authenticated caller. Kept so roles that
    /// already hold it still resolve rather than being reported as invalid at startup.
    /// </summary>
    public const string DescriptorsLogPersistenceStrategies = "workflows/descriptors/log-persistence-strategies";
    /// <summary>
    /// Formerly guarded browsing the output converters, which now requires only an authenticated caller. Kept so roles that
    /// already hold it still resolve rather than being reported as invalid at startup.
    /// </summary>
    public const string DescriptorsOutputConverters = "workflows/descriptors/output-converters";
    /// <summary>
    /// Formerly guarded browsing the workflow activation strategies, which now requires only an authenticated caller. Kept so roles that
    /// already hold it still resolve rather than being reported as invalid at startup.
    /// </summary>
    public const string DescriptorsActivationStrategies = "workflows/descriptors/activation-strategies";
    /// <summary>
    /// Formerly guarded the installed-features endpoints, which now require only an authenticated caller. Kept so
    /// roles that already hold it still resolve rather than being reported as invalid at startup.
    /// </summary>
    public const string SystemFeatures = "system/features";
}

/// <summary>Contributes the Workflows resources to the permission catalog.</summary>
[UsedImplicitly]
public sealed class WorkflowPermissionsDescriptorProvider : IPermissionDescriptorProvider
{
    /// <inheritdoc />
    public IEnumerable<PermissionDescriptor> GetDescriptors() =>
    [
        new(WorkflowPermissions.Definitions, [CoreVerbs.View, CoreVerbs.Write, CoreVerbs.Delete, CoreVerbs.Execute, "publish", "retract", "refresh", "reload"], "Workflow definitions", "Author, publish, run, and refresh workflow definitions.", "Workflows"),
        new(WorkflowPermissions.DefinitionVersions, [CoreVerbs.View, CoreVerbs.Delete, "revert"], "Workflow definition versions", "Delete and revert individual definition versions. Listing versions is covered by workflows/definitions:view; the view verb is retained so existing roles that hold it stay valid.", "Workflows"),
        new(WorkflowPermissions.Instances, [CoreVerbs.View, CoreVerbs.Write, CoreVerbs.Delete, "cancel"], "Workflow instances", "Inspect, import, delete, and cancel workflow instances.", "Workflows"),
        new(WorkflowPermissions.ActivityExecutions, [CoreVerbs.View], "Activity executions", "Inspect activity execution records and summaries.", "Workflows"),
        new(WorkflowPermissions.Runtime, [CoreVerbs.View, "control"], "Workflow runtime", "Inspect runtime status, and pause, resume, or drain the runtime.", "Workflows"),
        new(WorkflowPermissions.BookmarkQueueDeadLetters, [CoreVerbs.View, CoreVerbs.Delete, "replay"], "Bookmark queue dead letters", "Inspect, replay, and delete bookmark queue dead-letter items.", "Workflows"),
        new(WorkflowPermissions.Events, ["trigger"], "Workflow events", "Trigger workflow events.", "Workflows"),
        new(WorkflowPermissions.Tasks, ["complete"], "Workflow tasks", "Complete external workflow tasks.", "Workflows"),
        new(WorkflowPermissions.Tests, [CoreVerbs.Execute], "Activity tests", "Execute activity tests.", "Workflows"),
        new(WorkflowPermissions.DescriptorsActivities, [CoreVerbs.View], "Activity descriptors", "Required to refresh the activity registry (GET /descriptors/activities?refresh=true) and to resolve activity property options. Reading the activity catalog no longer requires it; roles that hold it stay valid.", "Workflows"),
        new(WorkflowPermissions.DescriptorsExpressions, [CoreVerbs.View], "Expression descriptors", "No longer required: any authenticated caller may read this catalog. Retained so existing roles that hold it stay valid.", "Workflows"),
        new(WorkflowPermissions.DescriptorsStorageDrivers, [CoreVerbs.View], "Storage driver descriptors", "No longer required: any authenticated caller may read this catalog. Retained so existing roles that hold it stay valid.", "Workflows"),
        new(WorkflowPermissions.DescriptorsVariables, [CoreVerbs.View], "Variable descriptors", "No longer required: any authenticated caller may read this catalog. Retained so existing roles that hold it stay valid.", "Workflows"),
        new(WorkflowPermissions.DescriptorsCommitStrategies, [CoreVerbs.View], "Commit strategy descriptors", "No longer required: any authenticated caller may read this catalog. Retained so existing roles that hold it stay valid.", "Workflows"),
        new(WorkflowPermissions.DescriptorsIncidentStrategies, [CoreVerbs.View], "Incident strategy descriptors", "No longer required: any authenticated caller may read this catalog. Retained so existing roles that hold it stay valid.", "Workflows"),
        new(WorkflowPermissions.DescriptorsLogPersistenceStrategies, [CoreVerbs.View], "Log persistence strategy descriptors", "No longer required: any authenticated caller may read this catalog. Retained so existing roles that hold it stay valid.", "Workflows"),
        new(WorkflowPermissions.DescriptorsOutputConverters, [CoreVerbs.View], "Output converter descriptors", "No longer required: any authenticated caller may read this catalog. Retained so existing roles that hold it stay valid.", "Workflows"),
        new(WorkflowPermissions.DescriptorsActivationStrategies, [CoreVerbs.View], "Activation strategy descriptors", "No longer required: any authenticated caller may read this catalog. Retained so existing roles that hold it stay valid.", "Workflows"),
        new(WorkflowPermissions.SystemFeatures, [CoreVerbs.View], "Installed features", "No longer required: any authenticated caller may list installed features. Retained so existing roles that hold it stay valid.", "Workflows"),
    ];
}
