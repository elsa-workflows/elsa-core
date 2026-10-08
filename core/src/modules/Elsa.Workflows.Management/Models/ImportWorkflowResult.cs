using Elsa.Workflows.Management.Entities;

namespace Elsa.Workflows.Management.Models;

/// <summary>
/// Represents the result of importing a workflow definition.
/// </summary>
/// <param name="Succeeded">True if the import was successful, otherwise false.</param>
/// <param name="WorkflowDefinition">The imported workflow definition.</param>
/// <param name="ValidationErrors">Any validation errors that occurred during the import.</param>
public record ImportWorkflowResult(bool Succeeded, WorkflowDefinition WorkflowDefinition, ICollection<WorkflowValidationError> ValidationErrors);