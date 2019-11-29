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
        private readonly IWorkflowRunner workflowRunner;
        private readonly IWorkflowInstanceStore workflowInstanceStore;

        public HttpWorkflowsController(
            ITokenService tokenService,
            IWorkflowRunner workflowRunner,
            IWorkflowInstanceStore workflowInstanceStore)
        {
            this.tokenService = tokenService;
            this.workflowRunner = workflowRunner;
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

            await workflowRunner.ResumeAsync(workflowInstance, input, cancellationToken: cancellationToken);

            return HttpContext.Items.ContainsKey(WorkflowHttpResult.Instance)
                ? (IActionResult)new EmptyResult()
                : Accepted();
        }
    }
}