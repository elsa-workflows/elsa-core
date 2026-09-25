using Elsa.Studio.Contracts;

namespace Elsa.Studio.Workflows.Domain.Notifications;

/// <summary>
/// Represents the notification published when bulk workflow definitions are retracted.
/// </summary>
public record BulkWorkflowDefinitionsRetracted(ICollection<string> WorkflowDefinitionIds) : INotification;