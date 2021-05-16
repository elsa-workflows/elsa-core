using System.Threading.Tasks;
using Elsa.Activities.Http.Bookmarks;
using Elsa.Activities.Http.Extensions;
using Elsa.Services;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Open.Linq.AsyncExtensions;

namespace Elsa.Activities.Http.Middleware
{
    public class HttpEndpointMiddleware
    {
        // TODO: Design multi-tenancy. 
        private const string? TenantId = default;
        private readonly RequestDelegate _next;

        public HttpEndpointMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext httpContext, IWorkflowLaunchpad workflowLaunchpad)
        {
            var path = httpContext.Request.Path.Value.ToLowerInvariant();
            var method = httpContext.Request.Method!.ToLowerInvariant();
            var cancellationToken = httpContext.RequestAborted;
            httpContext.Request.TryGetCorrelationId(out var correlationId);
            var useDispatch = httpContext.Request.GetUseDispatch();

            const string activityType = nameof(HttpEndpoint);
            var trigger = new HttpEndpointBookmark(path, method, null);
            var bookmark = new HttpEndpointBookmark(path, method, correlationId?.ToLowerInvariant());
            var collectWorkflowsContext = new CollectWorkflowsContext(activityType, bookmark, trigger, default, correlationId, default, TenantId);
            var pendingWorkflows = await workflowLaunchpad.CollectWorkflowsAsync(collectWorkflowsContext, cancellationToken).ToList();

            if (useDispatch)
            {
                await workflowLaunchpad.DispatchPendingWorkflowsAsync(pendingWorkflows, cancellationToken: cancellationToken);

                if (pendingWorkflows.Count > 0)
                {
                    httpContext.Response.ContentType = "application/json";
                    await httpContext.Response.WriteAsync(JsonConvert.SerializeObject(pendingWorkflows), cancellationToken);
                    return;
                }
            }
            else
            {
                await workflowLaunchpad.ExecutePendingWorkflowsAsync(pendingWorkflows, cancellationToken: cancellationToken);
                
                if (pendingWorkflows.Count > 0)
                    return;
            }
            
            await _next(httpContext);
        }
    }
}