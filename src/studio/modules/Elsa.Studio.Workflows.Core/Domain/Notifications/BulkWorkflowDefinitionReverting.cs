using Elsa.Studio.Workflows.Domain.Models;

namespace Elsa.Studio.Workflows.Domain.Notifications;

/// <summary>
/// Represents the notification published when bulk workflow definition are reverting.
/// </summary>
public record BulkWorkflowDefinitionReverting(ICollection<WorkflowDefinitionVersion> WorkflowDefinitionVersions);