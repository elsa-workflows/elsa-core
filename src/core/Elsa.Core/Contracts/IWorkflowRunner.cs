using Elsa.Models;
using Elsa.State;

namespace Elsa.Contracts;

public interface IWorkflowRunner
{
    Task<ExecuteWorkflowResult> RunAsync(Workflow workflow, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default);
    Task<ExecuteWorkflowResult> RunAsync(Workflow workflow, WorkflowState workflowState, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default);
    Task<ExecuteWorkflowResult> RunAsync(Workflow workflow, WorkflowState workflowState, Bookmark? bookmark, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default);
    Task<ExecuteWorkflowResult> RunAsync(WorkflowExecutionContext workflowExecutionContext);
}