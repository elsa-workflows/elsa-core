using Elsa.Workflows.Contracts;

namespace Elsa.Workflows.Services;

/// <summary>
/// Generates logger state for the specified <see cref="WorkflowExecutionContext"/>.
/// </summary>
public class WorkflowLoggerStateGenerator : ILoggerStateGenerator<WorkflowExecutionContext>
{
    /// <summary>
    /// Generates logger state for the specified <see cref="WorkflowExecutionContext"/>.
    /// </summary>
    /// <param name="workflowExecutionContext">The <see cref="WorkflowExecutionContext"/> to generate logger state for.</param>
    /// <returns>A <see cref="Dictionary{String, Object}" /> containing the state related to the <see cref="WorkflowExecutionContext"/>.</returns>
    public Dictionary<string, object> GenerateLoggerState(WorkflowExecutionContext workflowExecutionContext)
    {
        return new Dictionary<string, object>
        {
            ["WorkflowInstanceId"] = workflowExecutionContext.Id
        };
    }
}
