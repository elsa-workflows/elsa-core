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
    /// <summary>Inspect, delete, and revert individual definition versions.</summary>
    public const string DefinitionVersions = "workflows/definitions/versions";
    /// <summary>View and change the labels applied to a workflow definition.</summary>
    public const string DefinitionLabels = "workflows/definitions/labels";
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
    /// <summary>Browse available activity types and their options.</summary>
    public const string DescriptorsActivities = "workflows/descriptors/activities";
    /// <summary>Browse available expression types.</summary>
    public const string DescriptorsExpressions = "workflows/descriptors/expressions";
    /// <summary>Browse available storage drivers.</summary>
    public const string DescriptorsStorageDrivers = "workflows/descriptors/storage-drivers";
    /// <summary>Browse available variable types.</summary>
    public const string DescriptorsVariables = "workflows/descriptors/variables";
    /// <summary>Browse available commit strategies.</summary>
    public const string DescriptorsCommitStrategies = "workflows/descriptors/commit-strategies";
    /// <summary>Browse available incident strategies.</summary>
    public const string DescriptorsIncidentStrategies = "workflows/descriptors/incident-strategies";
    /// <summary>Browse available log persistence strategies.</summary>
    public const string DescriptorsLogPersistenceStrategies = "workflows/descriptors/log-persistence-strategies";
    /// <summary>Browse available output converters.</summary>
    public const string DescriptorsOutputConverters = "workflows/descriptors/output-converters";
    /// <summary>Browse available workflow activation strategies.</summary>
    public const string DescriptorsActivationStrategies = "workflows/descriptors/activation-strategies";
    /// <summary>Inspect which features are installed in this deployment.</summary>
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
        new(WorkflowPermissions.DefinitionVersions, [CoreVerbs.View, CoreVerbs.Delete, "revert"], "Workflow definition versions", "Inspect, delete, and revert individual definition versions.", "Workflows"),
        new(WorkflowPermissions.DefinitionLabels, [CoreVerbs.View, CoreVerbs.Update], "Workflow definition labels", "View and change the labels applied to a workflow definition.", "Workflows"),
        new(WorkflowPermissions.Instances, [CoreVerbs.View, CoreVerbs.Write, CoreVerbs.Delete, "cancel"], "Workflow instances", "Inspect, import, delete, and cancel workflow instances.", "Workflows"),
        new(WorkflowPermissions.ActivityExecutions, [CoreVerbs.View], "Activity executions", "Inspect activity execution records and summaries.", "Workflows"),
        new(WorkflowPermissions.Runtime, [CoreVerbs.View, "control"], "Workflow runtime", "Inspect runtime status, and pause, resume, or drain the runtime.", "Workflows"),
        new(WorkflowPermissions.BookmarkQueueDeadLetters, [CoreVerbs.View, CoreVerbs.Delete, "replay"], "Bookmark queue dead letters", "Inspect, replay, and delete bookmark queue dead-letter items.", "Workflows"),
        new(WorkflowPermissions.Events, ["trigger"], "Workflow events", "Trigger workflow events.", "Workflows"),
        new(WorkflowPermissions.Tasks, ["complete"], "Workflow tasks", "Complete external workflow tasks.", "Workflows"),
        new(WorkflowPermissions.Tests, [CoreVerbs.Execute], "Activity tests", "Execute activity tests.", "Workflows"),
        new(WorkflowPermissions.DescriptorsActivities, [CoreVerbs.View], "Activity descriptors", "Browse available activity types and their options.", "Workflows"),
        new(WorkflowPermissions.DescriptorsExpressions, [CoreVerbs.View], "Expression descriptors", "Browse available expression types.", "Workflows"),
        new(WorkflowPermissions.DescriptorsStorageDrivers, [CoreVerbs.View], "Storage driver descriptors", "Browse available storage drivers.", "Workflows"),
        new(WorkflowPermissions.DescriptorsVariables, [CoreVerbs.View], "Variable descriptors", "Browse available variable types.", "Workflows"),
        new(WorkflowPermissions.DescriptorsCommitStrategies, [CoreVerbs.View], "Commit strategy descriptors", "Browse available commit strategies.", "Workflows"),
        new(WorkflowPermissions.DescriptorsIncidentStrategies, [CoreVerbs.View], "Incident strategy descriptors", "Browse available incident strategies.", "Workflows"),
        new(WorkflowPermissions.DescriptorsLogPersistenceStrategies, [CoreVerbs.View], "Log persistence strategy descriptors", "Browse available log persistence strategies.", "Workflows"),
        new(WorkflowPermissions.DescriptorsOutputConverters, [CoreVerbs.View], "Output converter descriptors", "Browse available output converters.", "Workflows"),
        new(WorkflowPermissions.DescriptorsActivationStrategies, [CoreVerbs.View], "Activation strategy descriptors", "Browse available workflow activation strategies.", "Workflows"),
        new(WorkflowPermissions.SystemFeatures, [CoreVerbs.View], "Installed features", "Inspect which features are installed in this deployment.", "Workflows"),
    ];
}
