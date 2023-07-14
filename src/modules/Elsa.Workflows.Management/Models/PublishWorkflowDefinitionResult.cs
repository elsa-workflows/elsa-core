namespace Elsa.Workflows.Management.Models;

/// <summary>
/// Represents the result of publishing a workflow definition.
/// </summary>
public record PublishWorkflowDefinitionResult(bool Succeeded, ICollection<WorkflowValidationError> ValidationErrors);