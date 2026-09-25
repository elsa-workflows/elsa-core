using Elsa.Studio.Contracts;

namespace Elsa.Studio.Workflows.Domain.Notifications;

/// <summary>
/// Represents the notification published when a workflow definition id is retracting.
/// </summary>
public record WorkflowDefinitionIdRetracting(string WorkflowDefinitionId) : INotification;