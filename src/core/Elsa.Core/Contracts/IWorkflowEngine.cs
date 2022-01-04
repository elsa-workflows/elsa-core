using Elsa.Models;
using Elsa.State;

namespace Elsa.Contracts;

public interface IWorkflowEngine
{
    Task<ExecuteWorkflowResult> ExecuteAsync(Workflow workflow, CancellationToken cancellationToken = default);
    Task<ExecuteWorkflowResult> ExecuteAsync(Workflow workflow, WorkflowState workflowState, CancellationToken cancellationToken = default);
    Task<ExecuteWorkflowResult> ExecuteAsync(Workflow workflow, WorkflowState workflowState, Bookmark? bookmark, CancellationToken cancellationToken = default);
    Task<ExecuteWorkflowResult> ExecuteAsync(WorkflowExecutionContext workflowExecutionContext);

    WorkflowExecutionContext CreateWorkflowExecutionContext(
        IServiceProvider serviceProvider,
        Workflow workflow,
        WorkflowState? workflowState = default,
        Bookmark? bookmark = default,
        ExecuteActivityDelegate? executeActivityDelegate = default,
        CancellationToken cancellationToken = default);
}