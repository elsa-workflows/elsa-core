using System.Net.Mime;
using System.Text.Json;
using Elsa.Common.Models;
using Elsa.Http.Models;
using Elsa.Http.Options;
using Elsa.Http.Services;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Runtime.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Http.Middleware;

public class HttpTriggerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IBookmarkHasher _hasher;
    private readonly IWorkflowRuntime _workflowRuntime;
    private readonly IWorkflowHostFactory _workflowHostFactory;
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    private readonly HttpActivityOptions _options;
    private readonly string _activityTypeName = ActivityTypeNameHelper.GenerateTypeName<HttpEndpoint>();

    public HttpTriggerMiddleware(
        RequestDelegate next,
        IBookmarkHasher hasher,
        IWorkflowRuntime workflowRuntime,
        IWorkflowHostFactory workflowHostFactory,
        IWorkflowDefinitionService workflowDefinitionService,
        IOptions<HttpActivityOptions> options)
    {
        _next = next;
        _hasher = hasher;
        _workflowRuntime = workflowRuntime;
        _workflowHostFactory = workflowHostFactory;
        _workflowDefinitionService = workflowDefinitionService;
        _options = options.Value;
    }

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

        var request = httpContext.Request;
        var method = request.Method!.ToLowerInvariant();
        var cancellationToken = httpContext.RequestAborted;
        var routeData = GetRouteData(httpContext, routeMatcher, path);

        var requestModel = new HttpRequestModel(
            new Uri(request.GetEncodedUrl()),
            request.Path,
            request.Method,
            request.Query.ToDictionary(x => x.Key, x => x.Value.ToString()),
            routeData.Values,
            request.Headers.ToDictionary(x => x.Key, x => x.Value.ToString())
        );

        var input = new Dictionary<string, object> { [HttpEndpoint.InputKey] = requestModel };

        // TODO: Get correlation ID from query string or header etc.
        var correlationId = default(string);

        // Trigger the workflow.
        var bookmarkPayload = new HttpEndpointBookmarkPayload(path, method);
        var triggerOptions = new TriggerWorkflowsOptions(correlationId, input);

        var triggerResult = await _workflowRuntime.TriggerWorkflowsAsync(
            _activityTypeName,
            bookmarkPayload,
            triggerOptions,
            cancellationToken);

        // Check to see if we received any WriteHttpResponse activity bookmarks. If we do, acquire a lock on the workflow instance and resume it from here within an actual HTTP context so that the activity can complete its HTTP response.
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

            await workflowHost.ResumeWorkflowAsync(
                result.BookmarkId,
                null,
                cancellationToken);
            
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

    private static RouteData GetRouteData(HttpContext httpContext, IRouteMatcher routeMatcher, string path)
    {
        var routeData = httpContext.GetRouteData();
        var routeTable = httpContext.RequestServices.GetRequiredService<IRouteTable>();

        var matchingRouteQuery =
            from route in routeTable
            let routeValues = routeMatcher.Match(route, path)
            where routeValues != null
            select new { route, routeValues };

        var matchingRoute = matchingRouteQuery.FirstOrDefault();

        if (matchingRoute == null)
            return routeData;

        foreach (var (key, value) in matchingRoute.routeValues!)
            routeData.Values[key] = value;

        return routeData;
    }

    private string GetPath(HttpContext httpContext) => httpContext.Request.Path.Value.ToLowerInvariant();
}