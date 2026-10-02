using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Domain.Models;

namespace Elsa.Studio.Workflows.Domain.Notifications;

/// <summary>
/// Represents the notification published when bulk workflow definition versions are deleting.
/// </summary>
public record BulkWorkflowDefinitionVersionsDeleting(ICollection<WorkflowDefinitionVersion> Versions) : INotification;