using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Elsa.Activities.Http.Activities;
using Elsa.Activities.Http.Models;
using Elsa.Activities.Http.Services;
using Elsa.Core.Extensions;
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
            IWorkflowRegistry workflowRegistry,
            IWorkflowFactory workflowFactory,
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

            var workflowDefinition = workflowRegistry.GetById(workflowInstance.DefinitionId);
            var workflow = workflowFactory.CreateWorkflow(workflowDefinition, input, workflowInstance);
            var blockingSignalActivities = workflow.BlockingActivities.Where(x => x is SignalEvent).Cast<SignalEvent>().Where(x => x.SignalName == signal.Name).ToList();
            await workflowInvoker.ResumeAsync(workflow, blockingSignalActivities, cancellationToken);

            if (!context.Items.ContainsKey(WorkflowHttpResult.Instance))
            {
                context.Response.StatusCode = (int) HttpStatusCode.Accepted;
            }
        }
    }
}