using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Contracts;

namespace Elsa.Studio.Workflows.Domain.Notifications;

/// <summary>
/// Represents the notification published when an imported workflow is definition.
/// </summary>
public record ImportedWorkflowDefinition(WorkflowDefinition WorkflowDefinition) : INotification;