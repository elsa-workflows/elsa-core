using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Core.Services;

public interface IWorkflowRunner
{
    Task<InvokeWorkflowResult> RunAsync(IWorkflow workflow, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default);
    Task<InvokeWorkflowResult> RunAsync<T>(IDictionary<string, object>? input = default, CancellationToken cancellationToken = default) where T : IWorkflow;
    Task<InvokeWorkflowResult> RunAsync(Workflow workflow, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default);
    Task<InvokeWorkflowResult> RunAsync(Workflow workflow, WorkflowState workflowState, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default);
    Task<InvokeWorkflowResult> RunAsync(Workflow workflow, WorkflowState workflowState, Bookmark? bookmark, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default);
    Task<InvokeWorkflowResult> RunAsync(WorkflowExecutionContext workflowExecutionContext);
}