using System.IO;
using System.Net.Http;
using System.Net.Mime;
using System.Text.Json;
using Elsa.Http.Models;
using Elsa.Http.Options;
using Elsa.Http.Services;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Runtime.Services;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
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
    private readonly IHttpBookmarkProcessor _httpBookmarkProcessor;

    private readonly IRouteMatcher _routeMatcher;
    private readonly IRouteTable _routeTable;

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
        IHttpBookmarkProcessor httpBookmarkProcessor,
        IOptions<HttpActivityOptions> options,
        IRouteMatcher routeMatcher,
        IRouteTable routeTable

        )
    {
        _next = next;
        _workflowRuntime = workflowRuntime;
        _workflowHostFactory = workflowHostFactory;
        _workflowDefinitionService = workflowDefinitionService;
        _httpBookmarkProcessor = httpBookmarkProcessor;
        _options = options.Value;
        _routeMatcher = routeMatcher;
        _routeTable = routeTable;
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

        var matchingPath = GetMatchingRoute(path);

        var input = new Dictionary<string, object>
        {
            [HttpEndpoint.HttpContextInputKey] = true,
            [HttpEndpoint.RequestPathInputKey] = path
        };

        // TODO: Get correlation ID from query string or header etc.
        var correlationId = default(string);
        var request = httpContext.Request;
        var method = request.Method!.ToLowerInvariant();

        var bookmarkPayload = new HttpEndpointBookmarkPayload(matchingPath, method);
        var triggerOptions = new TriggerWorkflowsRuntimeOptions(correlationId, input);
        var cancellationToken = httpContext.RequestAborted;


        // Trigger the workflow.
        var triggerResult = await _workflowRuntime.TriggerWorkflowsAsync(_activityTypeName, bookmarkPayload, triggerOptions, cancellationToken);

        // Process the trigger result by resuming each HTTP bookmark, if any.
        await _httpBookmarkProcessor.ProcessBookmarks(triggerResult.TriggeredWorkflows, correlationId, input, cancellationToken);
    }

    private string? GetMatchingRoute(string? path) {

        var matchingRouteQuery =
            from route in _routeTable
            let routeValues = _routeMatcher.Match(route, path)
            where routeValues != null
            select new { route, routeValues };

        var matchingRoute = matchingRouteQuery.FirstOrDefault();
        var routeTemplate = matchingRoute?.route ?? path;

        return routeTemplate;
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