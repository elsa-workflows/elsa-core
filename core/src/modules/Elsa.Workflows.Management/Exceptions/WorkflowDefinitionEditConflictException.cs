namespace Elsa.Workflows.Management.Exceptions;

/// <summary>
/// The target version no longer matches the editable definition observed by the writer.
/// </summary>
public class WorkflowDefinitionEditConflictException() : InvalidOperationException("The workflow definition changed or is protected.");
