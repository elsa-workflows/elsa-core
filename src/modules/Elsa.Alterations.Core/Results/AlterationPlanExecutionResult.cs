using Elsa.Alterations.Core.Contracts;
using Elsa.Workflows.Core;

namespace Elsa.Alterations.Core.Results;

/// <summary>
/// The result of executing an alteration plan.
/// </summary>
public class AlterationPlanExecutionResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AlterationPlanExecutionResult"/> class.
    /// </summary>
    public AlterationPlanExecutionResult(IAlterationLog log)
    {
        Log = log;
    }

    /// <summary>
    /// An updated workflow execution context to which the alterations have been applied.
    /// </summary>
    public ICollection<WorkflowExecutionContext> ModifiedWorkflowExecutionContexts { get; } = new List<WorkflowExecutionContext>();

    /// <summary>
    /// The log entries that were created during the execution.
    /// </summary>
    public IAlterationLog Log { get; }

    /// <summary>
    /// Indicates whether the execution has succeeded.
    /// </summary>
    public bool HasSucceeded { get; set; } = true;
}