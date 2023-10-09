using Elsa.Http.Contracts;
using Elsa.Http.Models;
using Elsa.Http.Options;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Runtime.Contracts;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mime;
using System.Text.Json;
using Elsa.Extensions;
using Elsa.Http.Bookmarks;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Matches;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Results;

namespace Elsa.Http.Middleware;

/// <summary>
/// An ASP.NET middleware component that tries to match the inbound request path to an associated workflow and then run that workflow.
/// </summary>
[PublicAPI]
public class WorkflowsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWorkflowRuntime _workflowRuntime;
    private readonly IRouteMatcher _routeMatcher;
    private readonly IRouteTable _routeTable;
    private readonly IEnumerable<IHttpCorrelationIdSelector> _correlationIdSelectors;
    private readonly IEnumerable<IHttpWorkflowInstanceIdSelector> _workflowInstanceIdSelectors;
    private readonly IHttpBookmarkProcessor _httpBookmarkProcessor;
    private readonly IHttpEndpointFaultHandler _httpEndpointFaultHandler;
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
        IOptions<HttpActivityOptions> options,
        IHttpBookmarkProcessor httpBookmarkProcessor,
        IHttpEndpointFaultHandler httpEndpointFaultHandler,
        IHttpEndpointAuthorizationHandler httpEndpointAuthorizationHandler,
        IBookmarkStore bookmarkStore,
        ITriggerStore triggerStore,
        IBookmarkHasher hasher,
        IBookmarkPayloadSerializer serializer,
        IRouteMatcher routeMatcher,
        IRouteTable routeTable,
        IEnumerable<IHttpCorrelationIdSelector> correlationIdSelectors,
        IEnumerable<IHttpWorkflowInstanceIdSelector> workflowInstanceIdSelectors)
    {
        _next = next;
        _workflowRuntime = workflowRuntime;
        _options = options.Value;
        _httpBookmarkProcessor = httpBookmarkProcessor;
        _httpEndpointFaultHandler = httpEndpointFaultHandler;
        _httpEndpointAuthorizationHandler = httpEndpointAuthorizationHandler;
        _bookmarkStore = bookmarkStore;
        _triggerStore = triggerStore;
        _hasher = hasher;
        _serializer = serializer;
        _routeMatcher = routeMatcher;
        _routeTable = routeTable;
        _correlationIdSelectors = correlationIdSelectors;
        _workflowInstanceIdSelectors = workflowInstanceIdSelectors;
    }

    /// <summary>
    /// Attempts to matches the inbound request path to an associated workflow and then run that workflow.
    /// </summary>
    public async Task InvokeAsync(HttpContext httpContext)
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
            path = path[basePath.Value.Value!.Length..];
        }

        var matchingPath = GetMatchingRoute(path);

        var input = new Dictionary<string, object>
        {
            [HttpEndpoint.HttpContextInputKey] = true,
            [HttpEndpoint.RequestPathInputKey] = path
        };

        var cancellationToken = httpContext.RequestAborted;
        var request = httpContext.Request;
        var method = request.Method.ToLowerInvariant();
        var correlationId = await GetCorrelationIdAsync(httpContext, httpContext.RequestAborted);
        var workflowInstanceId = await GetWorkflowInstanceIdAsync(httpContext, httpContext.RequestAborted);
        var bookmarkPayload = new HttpEndpointBookmarkPayload(matchingPath, method);
        var triggerOptions = new TriggerWorkflowsOptions(correlationId, workflowInstanceId, default, input);
        var workflowsFilter = new WorkflowsFilter(_activityTypeName, bookmarkPayload, triggerOptions);
        var workflowMatches = (await _workflowRuntime.FindWorkflowsAsync(workflowsFilter, cancellationToken)).ToList();

        if (await HandleNoWorkflowsFoundAsync(httpContext, workflowMatches, basePath))
            return;

        if (await HandleMultipleWorkflowsFoundAsync(httpContext, workflowMatches, cancellationToken))
            return;

        var matchedWorkflow = workflowMatches.Single();

        if (await AuthorizeAsync(httpContext, matchedWorkflow, bookmarkPayload, cancellationToken))
            return;

        // Get settings from the bookmark payload.
        var foundBookmarkPayload = matchedWorkflow.Payload as HttpEndpointBookmarkPayload;
        
        // Get the configured request timeout, if any.
        var requestTimeout = foundBookmarkPayload?.RequestTimeout;

        if (requestTimeout == null)
        {
            // If no request timeout was configured, execute the workflow without a timeout.
            await ExecuteWorkflowAsync(matchedWorkflow, correlationId, input, requestTimeout, httpContext, cancellationToken);
            return;
        }

        // If a request timeout was configured, execute the workflow within the specified timeout.
        await ExecuteWorkflowWithinTimeoutAsync(matchedWorkflow, correlationId, input, requestTimeout.Value, httpContext);
    }

    private async Task ExecuteWorkflowWithinTimeoutAsync(WorkflowMatch matchedWorkflow, string? correlationId, IDictionary<string, object> input, TimeSpan requestTimeout, HttpContext httpContext)
    {
        using var cts = new CancellationTokenSource(requestTimeout);
        var originalCancellationToken = httpContext.RequestAborted;
        var systemCancellationToken = originalCancellationToken;
        httpContext.RequestAborted = cts.Token;
        var applicationCancellationToken = cts.Token;
        var cancellationTokens = new CancellationTokens(applicationCancellationToken, systemCancellationToken);

        await ExecuteWorkflowAsync(matchedWorkflow, correlationId, input, requestTimeout, httpContext, cancellationTokens);
    }

    private async Task ExecuteWorkflowAsync(
        WorkflowMatch matchedWorkflow,
        string? correlationId,
        IDictionary<string, object> input,
        TimeSpan? requestTimeout,
        HttpContext httpContext,
        CancellationTokens cancellationTokens)
    {
        var systemCancellationToken = cancellationTokens.SystemCancellationToken;
        var executionResult = await _workflowRuntime.ExecuteWorkflowAsync(matchedWorkflow, input, cancellationTokens);

        if (await HandleWorkflowFaultAsync(httpContext, executionResult, systemCancellationToken))
            return;

        // Process the trigger result by resuming each HTTP bookmark, if any.
        var affectedWorkflowStates = await _httpBookmarkProcessor.ProcessBookmarks(
            new List<WorkflowExecutionResult> { executionResult },
            correlationId,
            input,
            cancellationTokens);

        // Check if there were any errors.
        var faultedWorkflowState = affectedWorkflowStates.FirstOrDefault(x => x.SubStatus == WorkflowSubStatus.Faulted);

        if (faultedWorkflowState != null)
            await HandleWorkflowFaultAsync(httpContext, faultedWorkflowState, systemCancellationToken);
    }

    private string GetMatchingRoute(string path)
    {
        var matchingRouteQuery =
            from route in _routeTable
            let routeValues = _routeMatcher.Match(route, path)
            where routeValues != null
            select new { route, routeValues };

        var matchingRoute = matchingRouteQuery.FirstOrDefault();
        var routeTemplate = matchingRoute?.route ?? path;

        return routeTemplate;
    }

    private async Task<string?> GetCorrelationIdAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        var correlationId = default(string);

        foreach (var selector in _correlationIdSelectors.OrderByDescending(x => x.Priority))
        {
            correlationId = await selector.GetCorrelationIdAsync(httpContext, cancellationToken);

            if (correlationId != null)
                break;
        }

        return correlationId;
    }

    private async Task<string?> GetWorkflowInstanceIdAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        var workflowInstanceId = default(string);

        foreach (var selector in _workflowInstanceIdSelectors.OrderByDescending(x => x.Priority))
        {
            workflowInstanceId = await selector.GetWorkflowInstanceIdAsync(httpContext, cancellationToken);

            if (workflowInstanceId != null)
                break;
        }

        return workflowInstanceId;
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

    private string GetPath(HttpContext httpContext) => httpContext.Request.Path.Value!.ToLowerInvariant();

    private async Task<bool> HandleNoWorkflowsFoundAsync(HttpContext httpContext, ICollection<WorkflowMatch> workflowMatches, PathString? basePath)
    {
        if (workflowMatches.Any())
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

    private async Task<bool> HandleMultipleWorkflowsFoundAsync(HttpContext httpContext, ICollection<WorkflowMatch> workflowMatches, CancellationToken cancellationToken)
    {
        if (workflowMatches.Count <= 1)
            return false;

        httpContext.Response.ContentType = "application/json";
        httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var responseContent = JsonSerializer.Serialize(new
        {
            errorMessage = "The call is ambiguous and matches multiple workflows.",
            workflows = workflowMatches
        });

        await httpContext.Response.WriteAsync(responseContent, cancellationToken);
        return true;
    }

    private async Task<bool> HandleWorkflowFaultAsync(HttpContext httpContext, WorkflowExecutionResult workflowExecutionResult, CancellationToken cancellationToken)
    {
        var subStatus = workflowExecutionResult.SubStatus;

        if (subStatus != WorkflowSubStatus.Faulted || httpContext.Response.HasStarted)
            return false;

        var workflowState = (await _workflowRuntime.ExportWorkflowStateAsync(workflowExecutionResult.WorkflowInstanceId, cancellationToken))!;
        return await HandleWorkflowFaultAsync(httpContext, workflowState, cancellationToken);
    }

    private async Task<bool> HandleWorkflowFaultAsync(HttpContext httpContext, WorkflowState workflowState, CancellationToken cancellationToken)
    {
        await _httpEndpointFaultHandler.HandleAsync(new HttpEndpointFaultContext(httpContext, workflowState, cancellationToken));
        return true;
    }

    private async Task<bool> AuthorizeAsync(
        HttpContext httpContext,
        WorkflowMatch pendingWorkflowMatch,
        HttpEndpointBookmarkPayload bookmarkPayload,
        CancellationToken cancellationToken)
    {
        var payload = await GetBookmarkPayloadAsync(pendingWorkflowMatch, bookmarkPayload, cancellationToken);

        if (!(payload.Authorize ?? false))
            return false;

        var authorized = await _httpEndpointAuthorizationHandler.AuthorizeAsync(new AuthorizeHttpEndpointContext(httpContext, pendingWorkflowMatch.WorkflowInstanceId, payload.Policy));

        if (!authorized)
            httpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;

        return !authorized;
    }

    private async Task<HttpEndpointBookmarkPayload> GetBookmarkPayloadAsync(WorkflowMatch workflowMatch, HttpEndpointBookmarkPayload bookmarkPayload, CancellationToken cancellationToken)
    {
        var hash = _hasher.Hash(_activityTypeName, bookmarkPayload);

        if (workflowMatch is StartableWorkflowMatch)
        {
            var triggerFilter = new TriggerFilter { Hash = hash };
            var trigger = (await _triggerStore.FindManyAsync(triggerFilter, cancellationToken)).First();
            return trigger.GetPayload<HttpEndpointBookmarkPayload>();
        }

        var bookmarkFilter = new BookmarkFilter { Hash = hash };
        var bookmark = (await _bookmarkStore.FindManyAsync(bookmarkFilter, cancellationToken)).First();
        return bookmark.GetPayload<HttpEndpointBookmarkPayload>();
    }
}