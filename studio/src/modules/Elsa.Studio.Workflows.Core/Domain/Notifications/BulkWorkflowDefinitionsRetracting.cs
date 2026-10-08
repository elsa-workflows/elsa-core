using Elsa.Studio.Contracts;

namespace Elsa.Studio.Workflows.Domain.Notifications;

/// <summary>
/// Represents the notification published when bulk workflow definitions are retracting.
/// </summary>
public record BulkWorkflowDefinitionsRetracting(ICollection<string> WorkflowDefinitionIds) : INotification;