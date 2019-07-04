using System.Net;
using System.Threading.Tasks;
using Elsa.Activities.Http.Models;
using Elsa.Activities.Http.Services;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Services;
using Microsoft.AspNetCore.Http;

namespace Elsa.Activities.Http.Middleware
{
    public class SignalMiddleware
    {
        public SignalMiddleware(RequestDelegate _)
        {
        }
        
        public async Task InvokeAsync(
            HttpContext context, 
            ISharedAccessSignatureService sharedAccessSignatureService,
            IWorkflowInvoker workflowInvoker, 
            IWorkflowInstanceStore workflowInstanceStore)
        {
            var cancellationToken = context.RequestAborted;
            var token = context.Request.Query["token"];
            
            if (!sharedAccessSignatureService.TryDecryptToken(token, out Signal signal))
            {
                context.Response.StatusCode = (int) HttpStatusCode.NotFound;
                return;
            }

            var workflowInstance = await workflowInstanceStore.GetByIdAsync(signal.WorkflowInstanceId, cancellationToken);

            if (workflowInstance == null)
            {
                context.Response.StatusCode = (int) HttpStatusCode.NotFound;
                return;
            }                
            
            var input = new Variables
            {
                ["signal"] = signal.Name
            };

            await workflowInvoker.InvokeAsync(workflowInstance, input, new[] { signal.ActivityId }, cancellationToken);

            if (!context.Items.ContainsKey(WorkflowHttpResult.Instance))
            {
                context.Response.StatusCode = (int) HttpStatusCode.Accepted;
            }
        }
    }
}