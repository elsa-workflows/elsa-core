using Elsa.Http.Contracts;
using Elsa.Http.Models;
using Elsa.Http.Options;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mime;
using System.Text.Json;

namespace Elsa.Http.Middleware;

/// <summary>
/// An ASP.NET middleware component that tries to match the inbound request path to an associated workflow and then run that workflow.
/// </summary>
[PublicAPI]
public class WorkflowsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWorkflowRuntime _workflowRuntime;
    private readonly IWorkflowInstanceStore _workflowInstanceStore;
    private readonly IHttpBookmarkProcessor _httpBookmarkProcessor;
    private readonly IHttpEndpointWorkflowFaultHandler _httpEndpointWorkflowFaultHandler;
    private readonly IHttpEndpointAuthorizationHandler _httpEndpointAuthorizationHandler;
    private readonly IBookmarkStore _bookmarkStore;
    private readonly ITriggerStore _triggerStore;
    private readonly IBookmarkHasher _hasher;
    private readonly IBookmarkPayloadSerializer _serializer;
    private readonly HttpActivityOptions _options;
    private readonly string _activityTypeName = ActivityTypeNameHelper.GenerateTypeName<HttpEndpoint>();

    /// <summary>
    /// Constructor.
    /// </summary>
    public WorkflowsMiddleware(
        RequestDelegate next,
        IWorkflowRuntime workflowRuntime,
        IWorkflowInstanceStore workflowInstanceStore,
        IOptions<HttpActivityOptions> options,
        IHttpBookmarkProcessor httpBookmarkProcessor,
        IHttpEndpointWorkflowFaultHandler httpEndpointWorkflowFaultHandler,
        IHttpEndpointAuthorizationHandler httpEndpointAuthorizationHandler,
        IBookmarkStore bookmarkStore,
        ITriggerStore triggerStore,
        IBookmarkHasher hasher,
        IBookmarkPayloadSerializer serializer)
    {
        _next = next;
        _workflowRuntime = workflowRuntime;
        _workflowInstanceStore = workflowInstanceStore;
        _httpBookmarkProcessor = httpBookmarkProcessor;
        _httpEndpointWorkflowFaultHandler = httpEndpointWorkflowFaultHandler;
        _httpEndpointAuthorizationHandler = httpEndpointAuthorizationHandler;
        _bookmarkStore = bookmarkStore;
        _triggerStore = triggerStore;
        _hasher = hasher;
        _serializer = serializer;
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
            [HttpEndpoint.HttpContextInputKey] = true,
            [HttpEndpoint.RequestPathInputKey] = path
        };

        // TODO: Get correlation ID from query string or header etc.
        var correlationId = default(string);
        var request = httpContext.Request;
        var method = request.Method!.ToLowerInvariant();
        var bookmarkPayload = new HttpEndpointBookmarkPayload(path, method);
        var triggerOptions = new TriggerWorkflowsRuntimeOptions(correlationId, input);
        var cancellationToken = httpContext.RequestAborted;

        var workflowsQuery = new WorkflowsQuery(_activityTypeName, bookmarkPayload, triggerOptions);
        var pendingWorkflows = await _workflowRuntime.FindWorkflowsAsync(workflowsQuery, cancellationToken);

        if (await HandleNoWorkflowsFoundAsync(httpContext, pendingWorkflows, basePath))
            return;

        if (await HandleMultipleWorkflowsFoundAsync(httpContext, pendingWorkflows, cancellationToken))
            return;

        if (await HandleWorkflowFaultAsync(httpContext, pendingWorkflows.Single(), cancellationToken))
            return;

        if (await AuthorizeAsync(httpContext, pendingWorkflows.Single(), bookmarkPayload, cancellationToken))
            return;

        var executionResult = await _workflowRuntime.ExecutePendingWorkflowAsync(pendingWorkflows.Single(), input, cancellationToken);

        // Process the trigger result by resuming each HTTP bookmark, if any.
        await _httpBookmarkProcessor.ProcessBookmarks(new List<WorkflowExecutionResult> { executionResult }, correlationId, input, cancellationToken);
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

    private async Task<bool> HandleNoWorkflowsFoundAsync(HttpContext httpContext, IEnumerable<CollectedWorkflow> pendingWorkflows, PathString? basePath)
    {
        if (pendingWorkflows.Any())
            return false;

        // If a base path was configured, we are sure the requester tried to execute a workflow that doesn't exist.
        // Therefore, sending a 404 response seems appropriate instead of continuing with any subsequent middlewares.
        if (basePath != null)
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
            return true;
        }

        // If no base path was configured on the other hand, the request could be targeting anything else and should be handled by subsequent middlewares. 
        await _next(httpContext);

        return true;
    }

    private async Task<bool> HandleMultipleWorkflowsFoundAsync(HttpContext httpContext, IEnumerable<CollectedWorkflow> pendingWorkflows, CancellationToken cancellationToken)
    {
        if (pendingWorkflows.ToList().Count <= 1)
            return false;

        httpContext.Response.ContentType = "application/json";
        httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var responseContent = JsonSerializer.Serialize(new
        {
            errorMessage = "The call is ambiguous and matches multiple workflows.",
            workflows = pendingWorkflows
        });

        await httpContext.Response.WriteAsync(responseContent, cancellationToken);
        return true;
    }

    private async Task<bool> HandleWorkflowFaultAsync(HttpContext httpContext, CollectedWorkflow pendingWorkflow, CancellationToken cancellationToken)
    {
        var instanceFilter = new WorkflowInstanceFilter { Id = pendingWorkflow.WorkflowInstanceId };
        var workflowInstance = await _workflowInstanceStore.FindAsync(instanceFilter, cancellationToken);

        if (workflowInstance is not null
            && workflowInstance.SubStatus == WorkflowSubStatus.Faulted
            && !httpContext.Response.HasStarted)
        {
            await _httpEndpointWorkflowFaultHandler.HandleAsync(new HttpEndpointFaultedWorkflowContext(httpContext, workflowInstance, null, cancellationToken));
            return true;
        }

        return false;
    }

    private async Task<bool> AuthorizeAsync(
        HttpContext httpContext,
        CollectedWorkflow pendingWorkflow,
        HttpEndpointBookmarkPayload bookmarkPayload,
        CancellationToken cancellationToken = default)
    {
        var hash = _hasher.Hash(_activityTypeName, bookmarkPayload);
        var payload = default(HttpEndpointBookmarkPayload);

        if (pendingWorkflow is CollectedStartableWorkflow)
        {
            var triggerFilter = new TriggerFilter() { Hash = hash };
            var triggers = (await _triggerStore.FindManyAsync(triggerFilter, cancellationToken))
            .Select(x => _serializer.Deserialize<HttpEndpointBookmarkPayload>(x.Data!)).ToList();
            payload = triggers.Single();
        }
        else
        {
            var bookmarkFilter = new BookmarkFilter() { Hash = hash };
            var bookmarks = (await _bookmarkStore.FindManyAsync(bookmarkFilter, cancellationToken))
                .Select(x => _serializer.Deserialize<HttpEndpointBookmarkPayload>(x.Data!)).ToList();
            payload = bookmarks.Single();
        }

        if (!(payload.Authorize ?? false))
            return false;

        var authorized = await _httpEndpointAuthorizationHandler.AuthorizeAsync(new AuthorizeHttpEndpointContext(httpContext, pendingWorkflow.WorkflowInstanceId, payload.Policy));

        if (!authorized)
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        }

        return !authorized;
    }
}