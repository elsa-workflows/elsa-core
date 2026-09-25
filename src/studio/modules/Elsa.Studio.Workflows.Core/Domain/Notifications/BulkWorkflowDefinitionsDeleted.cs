using Elsa.Studio.Contracts;

namespace Elsa.Studio.Workflows.Domain.Notifications;

/// <summary>
/// Represents the notification published when bulk workflow definitions are deleted.
/// </summary>
public record BulkWorkflowDefinitionsDeleted(ICollection<string> WorkflowDefinitionIds) : INotification;