using Elsa.Mediator.Contracts;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Models;

namespace Elsa.Workflows.Management.Notifications;

/// <summary>
/// A request to validate a workflow definition.
/// </summary>
/// <param name="Workflow">The workflow materialized from the definition.</param>
public record WorkflowDefinitionValidating(Workflow Workflow, ICollection<WorkflowValidationError> ValidationErrors) : INotification;