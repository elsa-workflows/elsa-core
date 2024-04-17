using Elsa.Workflows.Activities;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Results;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Invokes a workflow in a transactional manner (i.e., runs the workflow in the current context and not via the <see cref="IWorkflowRuntime"/>).
/// </summary>
public interface IWorkflowInvoker
{
    /// <summary>
    /// Invokes the workflow.
    /// </summary>
    Task<RunWorkflowResult> InvokeAsync(Workflow workflow, InvokeWorkflowOptions? options = default, CancellationToken cancellationToken = default);
}