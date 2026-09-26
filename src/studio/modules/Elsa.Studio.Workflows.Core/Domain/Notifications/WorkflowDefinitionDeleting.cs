using Elsa.Studio.Contracts;

namespace Elsa.Studio.Workflows.Domain.Notifications;

/// <summary>
/// Represents the notification published when a workflow definition is deleting.
/// </summary>
public record WorkflowDefinitionDeleting(string WorkflowDefinitionId) : INotification;