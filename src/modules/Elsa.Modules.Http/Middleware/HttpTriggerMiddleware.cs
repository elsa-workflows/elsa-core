using System;
using System.Linq;
using System.Net.Mime;
using System.Text.Json;
using System.Threading.Tasks;
using Elsa.Contracts;
using Elsa.Helpers;
using Elsa.Modules.Http.Models;
using Elsa.Runtime.Contracts;
using Elsa.Runtime.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace Elsa.Modules.Http.Middleware;

public class HttpTriggerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IHasher _hasher;

    public HttpTriggerMiddleware(RequestDelegate next, IHasher hasher)
    {
        _next = next;
        _hasher = hasher;
    }

    public async Task InvokeAsync(HttpContext httpContext, IWorkflowServer workflowServer)
    {
        var path = GetPath(httpContext);
        var request = httpContext.Request;
        var method = request.Method!.ToLowerInvariant();
        var abortToken = httpContext.RequestAborted;
        var hash = _hasher.Hash(new HttpBookmarkData(path, method));
        var activityTypeName = TypeNameHelper.GenerateTypeName<HttpEndpoint>();
        var requestModel = new HttpRequestModel(new Uri(request.GetEncodedUrl()));
        var input = new { HttpRequest = requestModel };
        var stimulus = Stimulus.Standard(activityTypeName, hash, input);
        var executionResults = (await workflowServer.ExecuteStimulusAsync(stimulus, abortToken)).ToList();

        if (!executionResults.Any())
        {
            await _next(httpContext);
            return;
        }

        var response = httpContext.Response;

        if (!response.HasStarted)
        {
            response.ContentType = MediaTypeNames.Application.Json;
            response.StatusCode = StatusCodes.Status200OK;

            var model = new
            {
                workflowInstanceIds = executionResults.Select(x => x.ExecuteWorkflowResult.WorkflowState.Id).ToArray()
            };

            var json = JsonSerializer.Serialize(model);
            await response.WriteAsync(json, abortToken);
        }
    }

    private string GetPath(HttpContext httpContext) => httpContext.Request.Path.Value.ToLowerInvariant();
}