using Elsa.Models;
using Elsa.State;

namespace Elsa.Services;

public interface IWorkflowRunner
{
    Task<InvokeWorkflowResult> RunAsync(Workflow workflow, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default);
    Task<InvokeWorkflowResult> RunAsync(Workflow workflow, WorkflowState workflowState, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default);
    Task<InvokeWorkflowResult> RunAsync(Workflow workflow, WorkflowState workflowState, Bookmark? bookmark, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default);
    Task<InvokeWorkflowResult> RunAsync(WorkflowExecutionContext workflowExecutionContext);
}