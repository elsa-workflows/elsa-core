using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Models;
using Elsa.Activities.Http.Services;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Services;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Activities.Http.Controllers
{
    [ApiController]
    [Route("workflows")]
    public class HttpWorkflowsController : ControllerBase
    {
        private readonly ISharedAccessSignatureService sharedAccessSignatureService;
        private readonly IWorkflowInvoker workflowInvoker;
        private readonly IWorkflowInstanceStore workflowInstanceStore;

        public HttpWorkflowsController(
            ISharedAccessSignatureService sharedAccessSignatureService,
            IWorkflowInvoker workflowInvoker, 
            IWorkflowInstanceStore workflowInstanceStore)
        {
            this.sharedAccessSignatureService = sharedAccessSignatureService;
            this.workflowInvoker = workflowInvoker;
            this.workflowInstanceStore = workflowInstanceStore;
        }
        
        [Route("trigger/{token}")]
        public async Task<IActionResult> Trigger(string token, CancellationToken cancellationToken)
        {
            if (!sharedAccessSignatureService.TryDecryptToken(token, out Signal signal))
                return NotFound();

            var workflowInstance = await workflowInstanceStore.GetByIdAsync(signal.WorkflowInstanceId, cancellationToken);

            if (workflowInstance == null)
                return NotFound();                
            
            var input = new Variables
            {
                ["signal"] = signal.Name
            };

            await workflowInvoker.InvokeAsync(workflowInstance, input, cancellationToken: cancellationToken);

            return HttpContext.Items.ContainsKey(WorkflowHttpResult.Instance) 
                ? (IActionResult) new EmptyResult() 
                : Accepted();
        }
    }
}