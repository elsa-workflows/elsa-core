using System.Linq;
using System.Threading.Tasks;
using Elsa.Activities.Http.Triggers;
using Elsa.Services;
using Elsa.Triggers;
using Microsoft.AspNetCore.Http;

namespace Elsa.Activities.Http.Middleware
{
    public class ReceiveHttpRequestMiddleware
    {
        private readonly RequestDelegate _next;

        public ReceiveHttpRequestMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext, IWorkflowSelector workflowSelector, IWorkflowRunner workflowRunner)
        {
            var path = httpContext.Request.Path;
            var method = httpContext.Request.Method;
            var cancellationToken = httpContext.RequestAborted;

            var results = await workflowSelector.SelectWorkflowsAsync<ReceiveHttpRequestTrigger>(
                x => x.Path == path && x.Method == null || x.Method == method,
                cancellationToken);

            var result = results.FirstOrDefault();
            
            if (result == null)
                await _next(httpContext);
            else
                await workflowRunner.RunWorkflowAsync(result.WorkflowBlueprint, result.ActivityId, cancellationToken: cancellationToken);
        }
    }
}