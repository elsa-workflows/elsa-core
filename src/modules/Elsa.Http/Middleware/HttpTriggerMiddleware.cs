using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Helpers;
using Elsa.Http.Models;
using Elsa.Http.Services;
using Elsa.Services;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Http.Middleware;

public class HttpTriggerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IHasher _hasher;

    public HttpTriggerMiddleware(RequestDelegate next, IHasher hasher)
    {
        _next = next;
        _hasher = hasher;
    }

    public async Task InvokeAsync(HttpContext httpContext, IWorkflowService workflowService, IRouteMatcher routeMatcher)
    {
        var path = GetPath(httpContext);
        var request = httpContext.Request;
        var method = request.Method!.ToLowerInvariant();
        var abortToken = httpContext.RequestAborted;
        var hash = _hasher.Hash(new HttpEndpointBookmarkData(path, method));
        var activityTypeName = ActivityTypeNameHelper.GenerateTypeName<HttpEndpoint>();
        var routeData = GetRouteData(httpContext, routeMatcher, path);

        var requestModel = new HttpRequestModel(
            new Uri(request.GetEncodedUrl()),
            request.Path,
            request.Method,
            request.Query.ToDictionary(x => x.Key, x => x.Value.ToString()),
            routeData.Values,
            request.Headers.ToDictionary(x => x.Key, x => x.Value.ToString())
        );

        var input = new Dictionary<string, object>() { [HttpEndpoint.InputKey] = requestModel };
        var stimulus = Stimulus.Standard(activityTypeName, hash, input);
        var executionResults = (await workflowService.ExecuteStimulusAsync(stimulus, abortToken)).ToList();

        if (!executionResults.Any())
        {
            await _next(httpContext);
            return;
        }

        await WriteResponseAsync(httpContext, executionResults, abortToken);
    }

    private static async Task WriteResponseAsync(HttpContext httpContext, IEnumerable<ExecuteWorkflowInstructionResult> executionResults, CancellationToken cancellationToken)
    {
        var response = httpContext.Response;

        if (!response.HasStarted)
        {
            response.ContentType = MediaTypeNames.Application.Json;
            response.StatusCode = StatusCodes.Status200OK;

            var model = new
            {
                workflowInstanceIds = executionResults.Select(x => x.InvokeWorkflowResult.WorkflowState.Id).ToArray()
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