using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Domain.Models;

namespace Elsa.Studio.Workflows.Domain.Notifications;

/// <summary>
/// Represents the notification published when a workflow definition version is deleting.
/// </summary>
public record WorkflowDefinitionVersionDeleting(WorkflowDefinitionVersion WorkflowDefinitionVersion) : INotification;