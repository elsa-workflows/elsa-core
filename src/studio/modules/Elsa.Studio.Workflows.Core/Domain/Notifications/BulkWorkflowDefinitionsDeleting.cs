using Elsa.Studio.Contracts;

namespace Elsa.Studio.Workflows.Domain.Notifications;

/// <summary>
/// Represents the notification published when bulk workflow definitions are deleting.
/// </summary>
public record BulkWorkflowDefinitionsDeleting(ICollection<string> WorkflowDefinitionIds) : INotification;