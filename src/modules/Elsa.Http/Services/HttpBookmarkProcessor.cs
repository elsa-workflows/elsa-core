using Elsa.Common.Models;
using Elsa.Http.Contracts;
using Elsa.Http.Models;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Results;
using Microsoft.AspNetCore.Http;

namespace Elsa.Http.Services;

/// <inheritdoc />
public class HttpBookmarkProcessor : IHttpBookmarkProcessor
{
    private readonly IWorkflowRuntime _workflowRuntime;
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    private readonly IWorkflowHostFactory _workflowHostFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IWorkflowInstanceManager _workflowInstanceManager;
    private readonly WorkflowStateMapper _workflowStateMapper;

    /// <summary>
    /// Constructor.
    /// </summary>
    public HttpBookmarkProcessor(
        IWorkflowRuntime workflowRuntime,
        IWorkflowDefinitionService workflowDefinitionService,
        IWorkflowHostFactory workflowHostFactory,
        IHttpContextAccessor httpContextAccessor,
        IWorkflowInstanceManager workflowInstanceManager,
        WorkflowStateMapper workflowStateMapper)
    {
        _workflowRuntime = workflowRuntime;
        _workflowDefinitionService = workflowDefinitionService;
        _workflowHostFactory = workflowHostFactory;
        _httpContextAccessor = httpContextAccessor;
        _workflowInstanceManager = workflowInstanceManager;
        _workflowStateMapper = workflowStateMapper;
    }
    
    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowState>> ProcessBookmarks(
        IEnumerable<WorkflowExecutionResult> executionResults,
        string? correlationId,
        IDictionary<string, object>? input,
        CancellationToken cancellationToken = default)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext == null)
            throw new Exception("Invalid use of this method, because there is no HTTP context");

        // We must assume that the workflow executed in a different process (when e.g. using Proto.Actor)
        // and check if we received any `HttpEndpoint` or `WriteHttpResponse` activity bookmarks.
        // If we did, acquire a lock on the workflow instance and resume it from here within an actual HTTP context so that the activity can complete its HTTP response.
        var httpEndpointTypeName = ActivityTypeNameHelper.GenerateTypeName<HttpEndpoint>();
        var writeHttpResponseTypeName = ActivityTypeNameHelper.GenerateTypeName<WriteHttpResponse>();

        var query =
            from executionResult in executionResults
            from bookmark in executionResult.Bookmarks
            where bookmark.Name == writeHttpResponseTypeName 
                  || (bookmark.Name == httpEndpointTypeName
                      // If a "regular" HttpEndpointBookmarkPayload bookmark was created, we do not want to resume that - it's a bookmark that was created to block the workflow until the desired HTTP request comes in.
                      && bookmark.Payload is not HttpEndpointBookmarkPayload)
            select (InstanceId: executionResult.WorkflowInstanceId, bookmark.Id);

        var workflowExecutionResults = new Stack<(string InstanceId, string BookmarkId)>(query);
        var workflowStates = new List<WorkflowState>();

        while (workflowExecutionResults.TryPop(out var result))
        {
            // Resume the workflow "in-process".
            var workflowState = await _workflowRuntime.ExportWorkflowStateAsync(
                result.InstanceId,
                cancellationToken);

            if (workflowState == null)
            {
                // TODO: log this, shouldn't normally happen.
                continue;
            }

            var workflowDefinition = await _workflowDefinitionService.FindAsync(
                workflowState.DefinitionId,
                VersionOptions.SpecificVersion(workflowState.DefinitionVersion),
                cancellationToken);

            if (workflowDefinition == null)
            {
                // TODO: Log this, shouldn't normally happen.
                continue;
            }

            var workflow = await _workflowDefinitionService.MaterializeWorkflowAsync(
                workflowDefinition,
                cancellationToken);
            
            var workflowHost = await _workflowHostFactory.CreateAsync(workflow, workflowState, cancellationToken);
            var options = new ResumeWorkflowHostOptions(correlationId, result.BookmarkId, Input: input);
            await workflowHost.ResumeWorkflowAsync(options, cancellationToken);
            
            // Import the updated workflow state into the runtime.
            workflowState = workflowHost.WorkflowState;
            await _workflowRuntime.ImportWorkflowStateAsync(workflowState, cancellationToken);
            workflowStates.Add(workflowState);
        }

        // Save the updated workflow states.
        foreach (var workflowInstance in workflowStates.Select(workflowState => _workflowStateMapper.Map(workflowState)!)) 
            await _workflowInstanceManager.SaveAsync(workflowInstance, cancellationToken);

        return workflowStates;
    }
}