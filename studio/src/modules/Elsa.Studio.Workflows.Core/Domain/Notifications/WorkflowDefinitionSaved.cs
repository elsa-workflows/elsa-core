using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Contracts;

namespace Elsa.Studio.Workflows.Domain.Notifications;

/// Represents a notification sent when a workflow definition has been saved.
public record WorkflowDefinitionSaved(WorkflowDefinition WorkflowDefinition) : INotification;