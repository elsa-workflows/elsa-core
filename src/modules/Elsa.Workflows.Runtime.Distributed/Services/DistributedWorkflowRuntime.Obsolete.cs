using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Matches;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Parameters;
using Elsa.Workflows.Runtime.Params;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Results;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Runtime.Distributed;

public partial class DistributedWorkflowRuntime
{
    private readonly ObsoleteWorkflowRuntime _obsoleteApi;

    public Task<CanStartWorkflowResult> CanStartWorkflowAsync(string definitionId, StartWorkflowRuntimeParams? options = null) => _obsoleteApi.CanStartWorkflowAsync(definitionId, options);
    public Task<WorkflowExecutionResult> StartWorkflowAsync(string definitionId, StartWorkflowRuntimeParams? options = null) => _obsoleteApi.StartWorkflowAsync(definitionId, options);
    public Task<ICollection<WorkflowExecutionResult>> StartWorkflowsAsync(string activityTypeName, object bookmarkPayload, TriggerWorkflowsOptions? options = null) => _obsoleteApi.StartWorkflowsAsync(activityTypeName, bookmarkPayload, options);
    public Task<WorkflowExecutionResult?> TryStartWorkflowAsync(string definitionId, StartWorkflowRuntimeParams? options = null) => _obsoleteApi.TryStartWorkflowAsync(definitionId, options);
    public Task<WorkflowExecutionResult?> ResumeWorkflowAsync(string workflowInstanceId, ResumeWorkflowRuntimeParams? options = null) => _obsoleteApi.ResumeWorkflowAsync(workflowInstanceId, options);
    public Task<ICollection<WorkflowExecutionResult>> ResumeWorkflowsAsync(string activityTypeName, object bookmarkPayload, TriggerWorkflowsOptions? options = null) => _obsoleteApi.ResumeWorkflowsAsync(activityTypeName, bookmarkPayload, options);
    public Task<TriggerWorkflowsResult> TriggerWorkflowsAsync(string activityTypeName, object bookmarkPayload, TriggerWorkflowsOptions? options = null) => _obsoleteApi.TriggerWorkflowsAsync(activityTypeName, bookmarkPayload, options);
    public Task<WorkflowExecutionResult> ExecuteWorkflowAsync(WorkflowMatch match, ExecuteWorkflowParams? options = default) => _obsoleteApi.ExecuteWorkflowAsync(match, options);
    public Task<CancellationResult> CancelWorkflowAsync(string workflowInstanceId, CancellationToken cancellationToken = default) => _obsoleteApi.CancelWorkflowAsync(workflowInstanceId, cancellationToken);
    public Task<IEnumerable<WorkflowMatch>> FindWorkflowsAsync(WorkflowsFilter filter, CancellationToken cancellationToken = default) => _obsoleteApi.FindWorkflowsAsync(filter, cancellationToken);
    public Task<WorkflowState?> ExportWorkflowStateAsync(string workflowInstanceId, CancellationToken cancellationToken = default) => _obsoleteApi.ExportWorkflowStateAsync(workflowInstanceId, cancellationToken);
    public Task ImportWorkflowStateAsync(WorkflowState workflowState, CancellationToken cancellationToken = default) => _obsoleteApi.ImportWorkflowStateAsync(workflowState, cancellationToken);
    public Task UpdateBookmarkAsync(StoredBookmark bookmark, CancellationToken cancellationToken = default) => _obsoleteApi.UpdateBookmarkAsync(bookmark, cancellationToken);
    public Task<long> CountRunningWorkflowsAsync(CountRunningWorkflowsRequest request, CancellationToken cancellationToken = default) => _obsoleteApi.CountRunningWorkflowsAsync(request, cancellationToken);
}