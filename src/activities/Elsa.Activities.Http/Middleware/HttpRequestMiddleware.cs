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
        // TODO: Figure out how to start jobs across multiple tenants / how to get a list of all tenants. 
        private const string TenantId = default;
        private readonly RequestDelegate _next;

        public HttpEndpointMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext httpContext,
            IBookmarkFinder bookmarkFinder,
            ITriggerFinder triggerFinder,
            IWorkflowRunner workflowRunner,
            IWorkflowInstanceStore workflowInstanceStore,
            IContentSerializer contentSerializer)
        {
            var path = httpContext.Request.Path.Value.ToLowerInvariant();
            var method = httpContext.Request.Method!.ToLowerInvariant();
            var cancellationToken = httpContext.RequestAborted;
            httpContext.Request.TryGetCorrelationId(out var correlationId);

            // Find triggers.
            var triggers = (await triggerFinder.FindTriggersAsync(nameof(HttpEndpoint), new HttpEndpointBookmark(path, method, correlationId), TenantId, cancellationToken)).ToList();

            if (triggers.Count > 1)
            {
                httpContext.Response.StatusCode = (int) HttpStatusCode.BadRequest;
                await httpContext.Response.WriteAsync("Request matches multiple workflows.", cancellationToken);
                return;
            }

            if (triggers.Count == 1)
            {
                var trigger = triggers.First();
                var workflowBlueprint = trigger.WorkflowBlueprint;
                var activityId = trigger.ActivityId;
                var workflowInstance = await workflowRunner.RunWorkflowAsync(workflowBlueprint, activityId, cancellationToken: cancellationToken);
                await HandleWorkflowInstanceResponseAsync(httpContext, workflowInstance, contentSerializer, cancellationToken);
                return;
            }

            // Find bookmarks.
            var bookmarkQuery = new HttpEndpointBookmark(path, method, correlationId?.ToLowerInvariant());
            var bookmarks = await bookmarkFinder.FindBookmarksAsync<HttpEndpoint>(bookmarkQuery, TenantId, cancellationToken).ToList();

            if (!bookmarks.Any())
            {
                await _next(httpContext);
                return;
            }

            if (bookmarks.Count > 1)
            {
                httpContext.Response.StatusCode = (int) HttpStatusCode.BadRequest;
                await httpContext.Response.WriteAsync("Request matches multiple workflows.", cancellationToken);
            }
            else
            {
                var result = bookmarks.First();
                var workflowInstance = await workflowInstanceStore.FindByIdAsync(result.WorkflowInstanceId, cancellationToken);

                if (workflowInstance == null)
                {
                    await _next(httpContext);
                    return;
                }

                workflowInstance = await workflowRunner.RunWorkflowAsync(workflowInstance, result.ActivityId, cancellationToken: cancellationToken);

                await HandleWorkflowInstanceResponseAsync(httpContext, workflowInstance, contentSerializer, cancellationToken);
            }
        }

        private async Task HandleWorkflowInstanceResponseAsync(HttpContext httpContext, WorkflowInstance workflowInstance, IContentSerializer contentSerializer, CancellationToken cancellationToken)
        {
            if (workflowInstance.WorkflowStatus == WorkflowStatus.Faulted)
            {
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