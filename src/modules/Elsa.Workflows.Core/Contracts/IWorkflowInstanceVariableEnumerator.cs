namespace Elsa.Workflows;

/// <summary>
/// Enumerates variables for a workflow instance.
/// </summary>
public interface IWorkflowInstanceVariableEnumerator
{
    /// <summary>
    /// Enumerates all variables in the specified <see cref="workflowInstanceId"/>.
    /// </summary>
    Task<IEnumerable<ResolvedVariable>> EnumerateVariables(WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken = default);
}