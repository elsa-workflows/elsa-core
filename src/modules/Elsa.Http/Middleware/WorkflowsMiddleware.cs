using System.Net.Mime;
using System.Text.Json;
using Elsa.Common.Models;
using Elsa.Http.Models;
using Elsa.Http.Options;
using Elsa.Http.Services;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Runtime.Services;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Http.Middleware;

/// <summary>
/// An ASP.NET middleware component that tries to match the inbound request path to an associated workflow and then run that workflow.
/// </summary>
[PublicAPI]
public class WorkflowsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWorkflowRuntime _workflowRuntime;
    private readonly IWorkflowHostFactory _workflowHostFactory;
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    private readonly HttpActivityOptions _options;
    private readonly string _activityTypeName = ActivityTypeNameHelper.GenerateTypeName<HttpEndpoint>();

    /// <summary>
    /// Constructor.
    /// </summary>
    public WorkflowsMiddleware(
        RequestDelegate next,
        IWorkflowRuntime workflowRuntime,
        IWorkflowHostFactory workflowHostFactory,
        IWorkflowDefinitionService workflowDefinitionService,
        IOptions<HttpActivityOptions> options)
    {
        _next = next;
        _workflowRuntime = workflowRuntime;
        _workflowHostFactory = workflowHostFactory;
        _workflowDefinitionService = workflowDefinitionService;
        _options = options.Value;
    }

    
    /// <summary>
    /// Attempts to matches the inbound request path to an associated workflow and then run that workflow.
    /// </summary>
    public async Task InvokeAsync(HttpContext httpContext, IRouteMatcher routeMatcher)
    {
        var path = GetPath(httpContext);
        var basePath = _options.BasePath;

        // If the request path does not match the configured base path to handle workflows, then skip.
        if (!string.IsNullOrWhiteSpace(basePath))
        {
            if (!path.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
            {
                await _next(httpContext);
                return;
            }

            // Strip the base path.
            path = path.Substring(basePath.Value.Value.Length);
        }

        var input = new Dictionary<string, object>
        {
            [HttpEndpoint.HttpContextInputKey] = httpContext,
            [HttpEndpoint.RequestPathInputKey] = path
        };

        // TODO: Get correlation ID from query string or header etc.
        var correlationId = default(string);
        var request = httpContext.Request;
        var method = request.Method!.ToLowerInvariant();
        var bookmarkPayload = new HttpEndpointBookmarkPayload(path, method);
        var triggerOptions = new TriggerWorkflowsRuntimeOptions(correlationId, input);
        var cancellationToken = httpContext.RequestAborted;
        
        // Trigger the workflow.
        var triggerResult = await _workflowRuntime.TriggerWorkflowsAsync(
            _activityTypeName,
            bookmarkPayload,
            triggerOptions,
            cancellationToken);

        // We must assume that the workflow executed in a different process (when e.g. using Proto.Actor)
        // and check if we received any `WriteHttpResponse` activity bookmarks.
        // If we did, acquire a lock on the workflow instance and resume it from here within an actual HTTP context so that the activity can complete its HTTP response.
        var writeHttpResponseTypeName = ActivityTypeNameHelper.GenerateTypeName<WriteHttpResponse>();

        var query =
            from triggeredWorkflow in triggerResult.TriggeredWorkflows
            from bookmark in triggeredWorkflow.Bookmarks
            where bookmark.Name == writeHttpResponseTypeName
            select (triggeredWorkflow.InstanceId, bookmark.Id);

        var workflowExecutionResults = new Stack<(string InstanceId, string BookmarkId)>(query);

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
            var options = new ResumeWorkflowHostOptions(correlationId, result.BookmarkId);
            await workflowHost.ResumeWorkflowAsync(options, cancellationToken);
            
            // Import the updated workflow state into the runtime.
            await _workflowRuntime.ImportWorkflowStateAsync(workflowState, cancellationToken);
        }
    }

    private static async Task WriteResponseAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        var response = httpContext.Response;

        if (!response.HasStarted)
        {
            response.ContentType = MediaTypeNames.Application.Json;
            response.StatusCode = StatusCodes.Status200OK;

            var model = new
            {
                workflowInstanceIds = Array.Empty<string>(),
            };

            var json = JsonSerializer.Serialize(model);
            await response.WriteAsync(json, cancellationToken);
        }
    }
    
    private string GetPath(HttpContext httpContext) => httpContext.Request.Path.Value.ToLowerInvariant();
}