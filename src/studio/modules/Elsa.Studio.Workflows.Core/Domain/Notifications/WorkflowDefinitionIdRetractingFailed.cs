using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Domain.Models;

namespace Elsa.Studio.Workflows.Domain.Notifications;

/// <summary>
/// Represents the notification published when a workflow definition id retracting is failed.
/// </summary>
public record WorkflowDefinitionIdRetractingFailed(string WorkflowDefinitionId, ValidationErrors Errors) : INotification;