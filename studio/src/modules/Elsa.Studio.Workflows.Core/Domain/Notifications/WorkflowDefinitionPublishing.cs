using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Contracts;

namespace Elsa.Studio.Workflows.Domain.Notifications;

/// Represents a notification sent when a workflow definition is about to be published.
public record WorkflowDefinitionPublishing(WorkflowDefinition WorkflowDefinition) : INotification;