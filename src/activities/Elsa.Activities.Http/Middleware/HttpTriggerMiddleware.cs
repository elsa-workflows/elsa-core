using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Elsa.Contracts;
using Elsa.Helpers;
using Elsa.Runtime.Contracts;
using Elsa.Runtime.Models;
using Microsoft.AspNetCore.Http;

namespace Elsa.Activities.Http.Middleware;

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
        var method = httpContext.Request.Method!.ToLowerInvariant();
        var abortToken = httpContext.RequestAborted;
        var hash = _hasher.Hash((path.ToLowerInvariant(), method.ToLowerInvariant()));
        var activityTypeName = TypeNameHelper.GenerateTypeName<HttpTrigger>();
        var stimulus = Stimuli.Standard(activityTypeName, hash);
        var executionResults = (await workflowServer.ExecuteStimulusAsync(stimulus, abortToken)).ToList();
            
        if (!executionResults.Any())
        {
            await _next(httpContext);
            return;
        }

        var response = httpContext.Response;

        if (!response.HasStarted)
        {
            response.ContentType = "application/json";
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