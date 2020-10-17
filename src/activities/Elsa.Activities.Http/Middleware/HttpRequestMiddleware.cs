using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Services;
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

        public async Task InvokeAsync(HttpContext httpContext, IWorkflowSelector workflowSelector, IWorkflowHost workflowHost, CancellationToken cancellationToken)
        {
            var path = httpContext.Request.Path;
            var method = httpContext.Request.Method;
            
            var results = await workflowSelector.SelectWorkflowsAsync<ReceiveHttpRequestTrigger>(
                    x => x.Path == path && x.Method == null || x.Method == method,
                    cancellationToken).ToListAsync(cancellationToken);

            var result = results.FirstOrDefault();
            
            if (result == null)
                await _next(httpContext);
            else
                await workflowHost.RunWorkflowAsync(result.WorkflowBlueprint, result.ActivityId, cancellationToken: cancellationToken);
        }
    }
}