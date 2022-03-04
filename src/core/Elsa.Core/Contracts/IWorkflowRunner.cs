using Elsa.Models;
using Elsa.State;

namespace Elsa.Contracts;

public interface IWorkflowRunner
{
    Task<ExecuteWorkflowResult> RunAsync(Workflow workflow, IReadOnlyDictionary<string, object?>? input = default, CancellationToken cancellationToken = default);
    Task<ExecuteWorkflowResult> RunAsync(Workflow workflow, WorkflowState workflowState, IReadOnlyDictionary<string, object?>? input = default, CancellationToken cancellationToken = default);
    Task<ExecuteWorkflowResult> RunAsync(Workflow workflow, WorkflowState workflowState, Bookmark? bookmark, IReadOnlyDictionary<string, object?>? input = default, CancellationToken cancellationToken = default);
    Task<ExecuteWorkflowResult> RunAsync(WorkflowExecutionContext workflowExecutionContext);
}