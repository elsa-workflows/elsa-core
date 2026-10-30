using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Domain.Models;

namespace Elsa.Studio.Workflows.Domain.Notifications;

/// <summary>
/// Represents the notification published when a workflow definition retracting is failed.
/// </summary>
public record WorkflowDefinitionRetractingFailed(WorkflowDefinition WorkflowDefinition, ValidationErrors Errors) : INotification;