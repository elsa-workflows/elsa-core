using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Elsa.Activities.Http.Extensions;
using Elsa.Activities.Http.Triggers;
using Elsa.Persistence;
using Elsa.Services;
using Elsa.Triggers;
using Microsoft.AspNetCore.Http;
using Open.Linq.AsyncExtensions;

namespace Elsa.Activities.Http.Middleware
{
    public class ReceiveHttpRequestMiddleware
    {
        private readonly RequestDelegate _next;

        public ReceiveHttpRequestMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext, IWorkflowSelector workflowSelector, IWorkflowRunner workflowRunner, IWorkflowInstanceStore workflowInstanceStore)
        {
            var path = httpContext.Request.Path;
            var method = httpContext.Request.Method;
            var cancellationToken = httpContext.RequestAborted;
            httpContext.Request.TryGetCorrelationId(out var correlationId);

            var results = await workflowSelector.SelectWorkflowsAsync<ReceiveHttpRequestTrigger>(
                    x => AreSame(x.Path, path) && (x.Method == null || AreSame(x.Method, method)) && (x.CorrelationId == null && correlationId == null || x.CorrelationId == correlationId),
                    cancellationToken)
                .ToList();

            if (!results.Any())
            {
                await _next(httpContext);
                return;
            }

            if (results.Count > 1)
            {
                httpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                await httpContext.Response.WriteAsync("Request matches multiple workflows.", cancellationToken);
                return;
            }

            var result = results.Single();

            if (result.WorkflowInstanceId == null)
            {
                await workflowRunner.RunWorkflowAsync(result.WorkflowBlueprint, result.ActivityId, cancellationToken: cancellationToken);
            }
            else
            {
                var workflowInstance = await workflowInstanceStore.FindByIdAsync(result.WorkflowInstanceId, cancellationToken);

                if (workflowInstance == null)
                {
                    httpContext.Response.StatusCode = 404;
                    return;
                }

                await workflowRunner.RunWorkflowAsync(result.WorkflowBlueprint, workflowInstance, result.ActivityId, cancellationToken: cancellationToken);
            }
        }

        private static bool AreSame(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    }
}