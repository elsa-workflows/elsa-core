using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Bookmarks;
using Elsa.Activities.Http.Extensions;
using Elsa.Bookmarks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Serialization;
using Elsa.Services;
using Elsa.Triggers;
using Microsoft.AspNetCore.Http;
using Open.Linq.AsyncExtensions;

namespace Elsa.Activities.Http.Middleware
{
    public class HttpEndpointMiddleware
    {
        // TODO: Design multi-tenancy. 
        private const string TenantId = default;
        private readonly RequestDelegate _next;

        public HttpEndpointMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext httpContext, ITriggersWorkflows triggersWorkflows, IContentSerializer contentSerializer)
        {
            var path = httpContext.Request.Path.Value.ToLowerInvariant();
            var method = httpContext.Request.Method!.ToLowerInvariant();
            var cancellationToken = httpContext.RequestAborted;
            httpContext.Request.TryGetCorrelationId(out var correlationId);

            const string activityType = nameof(HttpEndpoint);
            var trigger = new HttpEndpointBookmark(path, method, null);
            var bookmark = new HttpEndpointBookmark(path, method, correlationId?.ToLowerInvariant());
            var workflowInstances = await triggersWorkflows.TriggerWorkflowsAsync(activityType, bookmark, trigger, correlationId, tenantId: TenantId, cancellationToken: cancellationToken).ToList();

            if (!workflowInstances.Any())
            {
                await _next(httpContext);
                return;
            }
            
            foreach (var workflowInstance in workflowInstances) 
                await HandleWorkflowInstanceResponseAsync(httpContext, workflowInstance, contentSerializer, cancellationToken);
        }

        private async Task HandleWorkflowInstanceResponseAsync(HttpContext httpContext, WorkflowInstance workflowInstance, IContentSerializer contentSerializer, CancellationToken cancellationToken)
        {
            if (workflowInstance.WorkflowStatus == WorkflowStatus.Faulted)
            {
                if (httpContext.Response.HasStarted)
                    return;
                
                httpContext.Response.StatusCode = (int) HttpStatusCode.InternalServerError;
                httpContext.Response.ContentType = "application/json";

                var model = new
                {
                    WorkflowInstanceId = workflowInstance.Id,
                    WorkflowStatus = workflowInstance.WorkflowStatus,
                    Fault = workflowInstance.Fault
                };

                await httpContext.Response.WriteAsync(contentSerializer.Serialize(model), cancellationToken);
            }
        }
    }
}