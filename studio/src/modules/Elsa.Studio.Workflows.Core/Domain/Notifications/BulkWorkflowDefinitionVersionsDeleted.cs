using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Domain.Models;

namespace Elsa.Studio.Workflows.Domain.Notifications;

/// <summary>
/// Represents the notification published when bulk workflow definition versions are deleted.
/// </summary>
public record BulkWorkflowDefinitionVersionsDeleted(ICollection<WorkflowDefinitionVersion> Versions) : INotification;