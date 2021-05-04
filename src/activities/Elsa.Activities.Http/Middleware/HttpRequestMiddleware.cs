using System.Threading.Tasks;
using Elsa.Activities.Http.Bookmarks;
using Elsa.Activities.Http.Extensions;
using Elsa.Dispatch;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Elsa.Activities.Http.Middleware
{
    public class HttpEndpointMiddleware
    {
        // TODO: Design multi-tenancy. 
        private const string? TenantId = default;
        private readonly RequestDelegate _next;

        public HttpEndpointMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext httpContext, IMediator mediator)
        {
            var path = httpContext.Request.Path.Value.ToLowerInvariant();
            var method = httpContext.Request.Method!.ToLowerInvariant();
            var cancellationToken = httpContext.RequestAborted;
            httpContext.Request.TryGetCorrelationId(out var correlationId);

            const string activityType = nameof(HttpEndpoint);
            var trigger = new HttpEndpointBookmark(path, method, null);
            var bookmark = new HttpEndpointBookmark(path, method, correlationId?.ToLowerInvariant());
            var workflowInstanceCount = await mediator.Send(new TriggerWorkflowsRequest(activityType, bookmark, trigger, default, correlationId, default, TenantId), cancellationToken);

            if (workflowInstanceCount == 0)
            {
                await _next(httpContext);
            }
        }
    }
}