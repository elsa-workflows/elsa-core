using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Core.Services;

public interface IWorkflowRunner
{
    Task<RunWorkflowResult> RunAsync(IActivity activity, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default);
    Task<RunWorkflowResult> RunAsync(IWorkflow workflow, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default);
    Task<RunWorkflowResult> RunAsync<T>(IDictionary<string, object>? input = default, CancellationToken cancellationToken = default) where T : IWorkflow;
    Task<RunWorkflowResult> RunAsync(string instanceId, IActivity activity, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default);
    Task<RunWorkflowResult> RunAsync(IWorkflow workflow, string instanceId, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default);
    Task<RunWorkflowResult> RunAsync<T>(string instanceId, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default) where T : IWorkflow;
    Task<RunWorkflowResult> RunAsync(Workflow workflow, string instanceId, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default);
    Task<RunWorkflowResult> RunAsync(Workflow workflow, WorkflowState workflowState, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default);
    Task<RunWorkflowResult> RunAsync(Workflow workflow, WorkflowState workflowState, string? bookmarkId, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default);
    Task<RunWorkflowResult> RunAsync(WorkflowExecutionContext workflowExecutionContext);
}