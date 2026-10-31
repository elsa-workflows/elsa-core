namespace Elsa.Studio.Workflows.Domain.Models;

/// <summary>
/// Represents the workflow import failure record.
/// </summary>
public record WorkflowImportFailure(string ErrorMessage, WorkflowImportFailureType FailureType);