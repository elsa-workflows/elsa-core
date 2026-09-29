using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Contracts;

namespace Elsa.Studio.Workflows.Domain.Notifications;

/// <summary>
/// Represents the notification published when a workflow definition is retracted.
/// </summary>
public record WorkflowDefinitionRetracted(WorkflowDefinition WorkflowDefinition) : INotification;