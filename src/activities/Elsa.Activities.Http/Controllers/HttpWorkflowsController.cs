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
        private readonly ITokenService tokenService;
        private readonly IWorkflowInvoker workflowInvoker;
        private readonly IWorkflowInstanceStore workflowInstanceStore;

        public HttpWorkflowsController(
            ITokenService tokenService,
            IWorkflowInvoker workflowInvoker,
            IWorkflowInstanceStore workflowInstanceStore)
        {
            this.tokenService = tokenService;
            this.workflowInvoker = workflowInvoker;
            this.workflowInstanceStore = workflowInstanceStore;
        }

        [Route("trigger/{token}")]
        [HttpGet, HttpPost]
        public async Task<IActionResult> Trigger(string token, CancellationToken cancellationToken)
        {
            if (!tokenService.TryDecryptToken(token, out Signal signal))
                return NotFound();

            var workflowInstance = await workflowInstanceStore.GetByIdAsync(signal.WorkflowInstanceId, cancellationToken);

            if (workflowInstance == null)
                return NotFound();

            var input = new Variables();
            input.SetVariable("Signal", signal.Name);

            await workflowInvoker.ResumeAsync(workflowInstance, input, cancellationToken: cancellationToken);

            return HttpContext.Items.ContainsKey(WorkflowHttpResult.Instance)
                ? (IActionResult)new EmptyResult()
                : Accepted();
        }
    }
}