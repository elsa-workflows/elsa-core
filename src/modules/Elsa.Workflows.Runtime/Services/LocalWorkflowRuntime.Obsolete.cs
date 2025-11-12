using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Matches;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Parameters;
using Elsa.Workflows.Runtime.Params;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Results;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Runtime;

public partial class LocalWorkflowRuntime
{
    private readonly Lazy<ObsoleteWorkflowRuntime> _obsoleteApi;
    private ObsoleteWorkflowRuntime ObsoleteApi => _obsoleteApi.Value;

    public Task<CanStartWorkflowResult> CanStartWorkflowAsync(string definitionId, StartWorkflowRuntimeParams? options = null) => ObsoleteApi.CanStartWorkflowAsync(definitionId, options);
    public Task<WorkflowExecutionResult> StartWorkflowAsync(string definitionId, StartWorkflowRuntimeParams? options = null) => ObsoleteApi.StartWorkflowAsync(definitionId, options);
    public Task<ICollection<WorkflowExecutionResult>> StartWorkflowsAsync(string activityTypeName, object bookmarkPayload, TriggerWorkflowsOptions? options = null) => ObsoleteApi.StartWorkflowsAsync(activityTypeName, bookmarkPayload, options);
    public Task<WorkflowExecutionResult?> TryStartWorkflowAsync(string definitionId, StartWorkflowRuntimeParams? options = null) => ObsoleteApi.TryStartWorkflowAsync(definitionId, options);
    public Task<WorkflowExecutionResult?> ResumeWorkflowAsync(string workflowInstanceId, ResumeWorkflowRuntimeParams? options = null) => ObsoleteApi.ResumeWorkflowAsync(workflowInstanceId, options);
    public Task<ICollection<WorkflowExecutionResult>> ResumeWorkflowsAsync(string activityTypeName, object bookmarkPayload, TriggerWorkflowsOptions? options = null) => ObsoleteApi.ResumeWorkflowsAsync(activityTypeName, bookmarkPayload, options);
    public Task<TriggerWorkflowsResult> TriggerWorkflowsAsync(string activityTypeName, object bookmarkPayload, TriggerWorkflowsOptions? options = null) => ObsoleteApi.TriggerWorkflowsAsync(activityTypeName, bookmarkPayload, options);
    public Task<WorkflowExecutionResult> ExecuteWorkflowAsync(WorkflowMatch match, ExecuteWorkflowParams? options = default) => ObsoleteApi.ExecuteWorkflowAsync(match, options);
    public Task<CancellationResult> CancelWorkflowAsync(string workflowInstanceId, CancellationToken cancellationToken = default) => ObsoleteApi.CancelWorkflowAsync(workflowInstanceId, cancellationToken);
    public Task<IEnumerable<WorkflowMatch>> FindWorkflowsAsync(WorkflowsFilter filter, CancellationToken cancellationToken = default) => ObsoleteApi.FindWorkflowsAsync(filter, cancellationToken);
    public Task<WorkflowState?> ExportWorkflowStateAsync(string workflowInstanceId, CancellationToken cancellationToken = default) => ObsoleteApi.ExportWorkflowStateAsync(workflowInstanceId, cancellationToken);
    public Task ImportWorkflowStateAsync(WorkflowState workflowState, CancellationToken cancellationToken = default) => ObsoleteApi.ImportWorkflowStateAsync(workflowState, cancellationToken);
    public Task UpdateBookmarkAsync(StoredBookmark bookmark, CancellationToken cancellationToken = default) => ObsoleteApi.UpdateBookmarkAsync(bookmark, cancellationToken);
    public Task<long> CountRunningWorkflowsAsync(CountRunningWorkflowsRequest request, CancellationToken cancellationToken = default) => ObsoleteApi.CountRunningWorkflowsAsync(request, cancellationToken);
}