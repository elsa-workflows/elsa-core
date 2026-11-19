namespace Elsa.Workflows.Management.Exceptions;

/// <summary>
/// An initial definition version could not be created because its identity was already in use.
/// </summary>
public class WorkflowDefinitionAlreadyExistsException() : InvalidOperationException("The workflow definition or version already exists.");
