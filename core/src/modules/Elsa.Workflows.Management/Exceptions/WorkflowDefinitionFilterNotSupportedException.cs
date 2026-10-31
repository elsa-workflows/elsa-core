namespace Elsa.Workflows.Management.Exceptions;

/// <summary>
/// Indicates that a requested workflow definition filter is not supported by the configured modules. The workflow-definition list endpoint reports this condition as HTTP 501.
/// </summary>
public class WorkflowDefinitionFilterNotSupportedException(string message) : NotSupportedException(message);
