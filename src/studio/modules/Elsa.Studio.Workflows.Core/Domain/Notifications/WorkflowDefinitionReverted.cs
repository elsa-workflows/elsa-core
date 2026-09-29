using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Contracts;

namespace Elsa.Studio.Workflows.Domain.Notifications;

/// <summary>
/// Represents the notification published when a workflow definition is reverted.
/// </summary>
public record WorkflowDefinitionReverted(WorkflowDefinition WorkflowDefinition) : INotification;