namespace Elsa.Workflows.Management.Notifications;

/// <summary>
/// Represents an update to a specific workflow definition version.
/// </summary>
public record WorkflowDefinitionVersionUpdate(string Id, string DefinitionId, bool UsableAsActivity);