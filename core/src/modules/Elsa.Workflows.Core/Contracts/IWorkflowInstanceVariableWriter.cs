namespace Elsa.Workflows;

public interface IWorkflowInstanceVariableWriter
{
    /// <summary>
    /// Sets the specified variables in the given <see cref="WorkflowExecutionContext"/>.
    /// </summary>
    /// <param name="workflowExecutionContext">The context of the workflow execution.</param>
    /// <param name="variables">The collection of variables to set.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A complete list of resolved variables.</returns>
    Task<IEnumerable<ResolvedVariable>> SetVariables(WorkflowExecutionContext workflowExecutionContext, IEnumerable<VariableUpdateValue> variables, CancellationToken cancellationToken = default);
}