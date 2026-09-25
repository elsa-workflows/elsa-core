using Elsa.Studio.Contracts;

namespace Elsa.Studio.Workflows.Domain.Notifications;

/// <summary>
/// Represents the notification published when a workflow definition is deleted.
/// </summary>
public record WorkflowDefinitionDeleted(string WorkflowDefinitionId) : INotification;