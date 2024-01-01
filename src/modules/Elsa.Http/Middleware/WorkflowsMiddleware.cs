using Elsa.Extensions;
using Elsa.Http.Bookmarks;
using Elsa.Http.Contracts;
using Elsa.Http.Models;
using Elsa.Http.Options;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Matches;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Results;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mime;
using System.Text.Json;
using Elsa.Extensions;
using Elsa.Http.Bookmarks;
using Elsa.Workflows;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Matches;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Results;
using Elsa.Workflows.State;

namespace Elsa.Http.Middleware;

/// <summary>
/// An ASP.NET middleware component that tries to match the inbound request path to an associated workflow and then run that workflow.
/// </summary>
[PublicAPI]
public class WorkflowsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly HttpActivityOptions _options;
    private readonly string _activityTypeName = ActivityTypeNameHelper.GenerateTypeName<HttpEndpoint>();

    /// <summary>
    /// Constructor.
    /// </summary>
    public WorkflowsMiddleware(
        RequestDelegate next,
        IOptions<HttpActivityOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    /// <summary>
    /// Attempts to matches the inbound request path to an associated workflow and then run that workflow.
    /// </summary>
    public async Task InvokeAsync(HttpContext httpContext, IServiceProvider serviceProvider)
    {
        var workflowRuntime = serviceProvider.GetRequiredService<IWorkflowRuntime>();

        var path = GetPath(httpContext);
        var basePath = _options.BasePath?.ToString().NormalizeRoute();

        // If the request path does not match the configured base path to handle workflows, then skip.
        if (!string.IsNullOrWhiteSpace(basePath))
        {
            if (!path.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
            {
                await _next(httpContext);
                return;
            }

            // Strip the base path.
            path = path[basePath.Length..];
        }

        var matchingPath = GetMatchingRoute(serviceProvider, path);

        var input = new Dictionary<string, object>
        {
            [HttpEndpoint.HttpContextInputKey] = true,
            [HttpEndpoint.RequestPathInputKey] = path
        };

        var cancellationToken = httpContext.RequestAborted;
        var request = httpContext.Request;
        var method = request.Method.ToLowerInvariant();
        var correlationId = await GetCorrelationIdAsync(serviceProvider, httpContext, httpContext.RequestAborted);
        var workflowInstanceId = await GetWorkflowInstanceIdAsync(serviceProvider, httpContext, httpContext.RequestAborted);
        var bookmarkPayload = new HttpEndpointBookmarkPayload(matchingPath, method);
        var triggerOptions = new TriggerWorkflowsOptions
        {
            CorrelationId = correlationId,
            WorkflowInstanceId = workflowInstanceId,
            Input = input
        };
        var workflowsFilter = new WorkflowsFilter(_activityTypeName, bookmarkPayload, triggerOptions);
        var workflowMatches = (await workflowRuntime.FindWorkflowsAsync(workflowsFilter, cancellationToken)).ToList();

        if (await HandleNoWorkflowsFoundAsync(httpContext, workflowMatches, basePath))
            return;

        if (await HandleMultipleWorkflowsFoundAsync(httpContext, workflowMatches, cancellationToken))
            return;

        var matchedWorkflow = workflowMatches.Single();

        if (await AuthorizeAsync(serviceProvider, httpContext, matchedWorkflow, bookmarkPayload, cancellationToken))
            return;

        // Get settings from the bookmark payload.
        var foundBookmarkPayload = matchedWorkflow.Payload as HttpEndpointBookmarkPayload;

        // Get the configured request timeout, if any.
        var requestTimeout = foundBookmarkPayload?.RequestTimeout;

        if (requestTimeout == null)
        {
            // If no request timeout was configured, execute the workflow without a timeout.
            await ExecuteWorkflowAsync(serviceProvider, workflowRuntime, matchedWorkflow, correlationId, input, requestTimeout, httpContext, cancellationToken);
            return;
        }

        // If a request timeout was configured, execute the workflow within the specified timeout.
        await ExecuteWorkflowWithinTimeoutAsync(serviceProvider, workflowRuntime, matchedWorkflow, correlationId, input, requestTimeout.Value, httpContext);
    }

    private async Task ExecuteWorkflowWithinTimeoutAsync(
        IServiceProvider serviceProvider,
        IWorkflowRuntime workflowRuntime,
        WorkflowMatch matchedWorkflow,
        string? correlationId,
        IDictionary<string, object> input,
        TimeSpan requestTimeout,
        HttpContext httpContext)
    {
        using var cts = new CancellationTokenSource(requestTimeout);
        var originalCancellationToken = httpContext.RequestAborted;
        var systemCancellationToken = originalCancellationToken;
        httpContext.RequestAborted = cts.Token;
        var applicationCancellationToken = cts.Token;
        var cancellationTokens = new CancellationTokens(applicationCancellationToken, systemCancellationToken);

        await ExecuteWorkflowAsync(serviceProvider, workflowRuntime, matchedWorkflow, correlationId, input, requestTimeout, httpContext, cancellationTokens);
    }

    private async Task ExecuteWorkflowAsync(
        IServiceProvider serviceProvider,
        IWorkflowRuntime workflowRuntime,
        WorkflowMatch matchedWorkflow,
        string? correlationId,
        IDictionary<string, object> input,
        TimeSpan? requestTimeout,
        HttpContext httpContext,
        CancellationTokens cancellationTokens)
    {
        var httpBookmarkProcessor = serviceProvider.GetRequiredService<IHttpBookmarkProcessor>();
        var systemCancellationToken = cancellationTokens.SystemCancellationToken;
        var executionOptions = new ExecuteWorkflowOptions
        {
            Input = input,
            CancellationTokens = cancellationTokens
        };
        var executionResult = await workflowRuntime.ExecuteWorkflowAsync(matchedWorkflow, executionOptions);

        if (await HandleWorkflowFaultAsync(serviceProvider, workflowRuntime, httpContext, executionResult, systemCancellationToken))
            return;

        // Process the trigger result by resuming each HTTP bookmark, if any.
        var affectedWorkflowStates = await httpBookmarkProcessor.ProcessBookmarks(
            new List<WorkflowExecutionResult> { executionResult },
            correlationId,
            input,
            cancellationTokens);

        // Check if there were any errors.
        var faultedWorkflowState = affectedWorkflowStates.FirstOrDefault(x => x.SubStatus == WorkflowSubStatus.Faulted);

        if (faultedWorkflowState != null)
            await HandleWorkflowFaultAsync(serviceProvider, httpContext, faultedWorkflowState, systemCancellationToken);
    }

    private string GetMatchingRoute(IServiceProvider serviceProvider, string path)
    {
        var routeMatcher = serviceProvider.GetRequiredService<IRouteMatcher>();
        var routeTable = serviceProvider.GetRequiredService<IRouteTable>();

        var matchingRouteQuery =
            from route in routeTable
            let routeValues = routeMatcher.Match(route, path)
            where routeValues != null
            select new { route, routeValues };

        var matchingRoute = matchingRouteQuery.FirstOrDefault();
        var routeTemplate = matchingRoute?.route ?? path;

        return routeTemplate;
    }

    private async Task<string?> GetCorrelationIdAsync(IServiceProvider serviceProvider, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var correlationIdSelectors = serviceProvider.GetServices<IHttpCorrelationIdSelector>();

        var correlationId = default(string);

        foreach (var selector in correlationIdSelectors.OrderByDescending(x => x.Priority))
        {
            correlationId = await selector.GetCorrelationIdAsync(httpContext, cancellationToken);

            if (correlationId != null)
                break;
        }

        return correlationId;
    }

    private async Task<string?> GetWorkflowInstanceIdAsync(IServiceProvider serviceProvider, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var workflowInstanceIdSelectors = serviceProvider.GetServices<IHttpWorkflowInstanceIdSelector>();

        var workflowInstanceId = default(string);

        foreach (var selector in workflowInstanceIdSelectors.OrderByDescending(x => x.Priority))
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

    private string GetPath(HttpContext httpContext) => httpContext.Request.Path.Value!.NormalizeRoute();

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

    private async Task<bool> HandleWorkflowFaultAsync(IServiceProvider serviceProvider, IWorkflowRuntime workflowRuntime, HttpContext httpContext, WorkflowExecutionResult workflowExecutionResult, CancellationToken cancellationToken)
    {
        var subStatus = workflowExecutionResult.SubStatus;

        if (subStatus != WorkflowSubStatus.Faulted || httpContext.Response.HasStarted)
            return false;

        var workflowState = (await workflowRuntime.ExportWorkflowStateAsync(workflowExecutionResult.WorkflowInstanceId, cancellationToken))!;
        return await HandleWorkflowFaultAsync(serviceProvider, httpContext, workflowState, cancellationToken);
    }

    private async Task<bool> HandleWorkflowFaultAsync(IServiceProvider serviceProvider, HttpContext httpContext, WorkflowState workflowState, CancellationToken cancellationToken)
    {
        var httpEndpointFaultHandler = serviceProvider.GetRequiredService<IHttpEndpointFaultHandler>();
        await httpEndpointFaultHandler.HandleAsync(new HttpEndpointFaultContext(httpContext, workflowState, cancellationToken));
        return true;
    }

    private async Task<bool> AuthorizeAsync(
        IServiceProvider serviceProvider,
        HttpContext httpContext,
        WorkflowMatch pendingWorkflowMatch,
        HttpEndpointBookmarkPayload bookmarkPayload,
        CancellationToken cancellationToken)
    {
        var httpEndpointAuthorizationHandler = serviceProvider.GetRequiredService<IHttpEndpointAuthorizationHandler>();
        var payload = await GetBookmarkPayloadAsync(serviceProvider, pendingWorkflowMatch, bookmarkPayload, cancellationToken);

        if (!(payload.Authorize ?? false))
            return false;

        var authorized = await httpEndpointAuthorizationHandler.AuthorizeAsync(new AuthorizeHttpEndpointContext(httpContext, pendingWorkflowMatch.WorkflowInstanceId, payload.Policy));

        if (!authorized)
            httpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;

        return !authorized;
    }

    private async Task<HttpEndpointBookmarkPayload> GetBookmarkPayloadAsync(IServiceProvider serviceProvider, WorkflowMatch workflowMatch, HttpEndpointBookmarkPayload bookmarkPayload, CancellationToken cancellationToken)
    {
        var bookmarkStore = serviceProvider.GetRequiredService<IBookmarkStore>();
        var triggerStore = serviceProvider.GetRequiredService<ITriggerStore>();
        var hasher = serviceProvider.GetRequiredService<IBookmarkHasher>();

        var hash = hasher.Hash(_activityTypeName, bookmarkPayload);

        if (workflowMatch is StartableWorkflowMatch)
        {
            var triggerFilter = new TriggerFilter { Hash = hash };
            var trigger = (await triggerStore.FindManyAsync(triggerFilter, cancellationToken)).First();
            return trigger.GetPayload<HttpEndpointBookmarkPayload>();
        }

        var bookmarkFilter = new BookmarkFilter { Hash = hash };
        var bookmark = (await bookmarkStore.FindManyAsync(bookmarkFilter, cancellationToken)).First();
        return bookmark.GetPayload<HttpEndpointBookmarkPayload>();
    }
}