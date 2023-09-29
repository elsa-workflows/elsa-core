using Elsa.Alterations.Core.Contracts;
using Elsa.Workflows.Core;

namespace Elsa.Alterations.Core.Results;

/// <summary>
/// The result of executing an alteration.
/// </summary>
public class AlterationExecutionResult
{
    public IAlterationLog Log { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AlterationExecutionResult"/> class.
    /// </summary>
    public AlterationExecutionResult(IAlterationLog log)
    {
        Log = log;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AlterationExecutionResult"/> class.
    /// </summary>
    public AlterationExecutionResult(IAlterationLog log, bool hasSucceeded) : this(log)
    {
        HasSucceeded = hasSucceeded;
    }

    /// <summary>
    /// An updated workflow execution context to which the alterations have been applied.
    /// </summary>
    public ICollection<WorkflowExecutionContext> ModifiedWorkflowExecutionContexts { get; } = new List<WorkflowExecutionContext>();

    /// <summary>
    /// Indicates whether the execution has succeeded.
    /// </summary>
    public bool HasSucceeded { get; set; } = true;
}