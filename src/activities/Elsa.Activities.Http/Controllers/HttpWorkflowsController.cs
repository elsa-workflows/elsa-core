using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Models;
using Elsa.Activities.Http.Services;
using Elsa.Queries;
using Elsa.Services;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Activities.Http.Controllers
{
    [ApiController]
    [Route("workflows")]
    public class HttpWorkflowsController : ControllerBase
    {
        private readonly ITokenService _tokenService;
        private readonly IWorkflowRunner _workflowRunner;
        private readonly IWorkflowInstanceManager _workflowInstanceManager;

        public HttpWorkflowsController(
            ITokenService tokenService,
            IWorkflowRunner workflowRunner,
            IWorkflowInstanceManager workflowInstanceManager)
        {
            _tokenService = tokenService;
            _workflowRunner = workflowRunner;
            _workflowInstanceManager = workflowInstanceManager;
        }

        [Route("trigger/{token}")]
        [HttpGet, HttpPost]
        public async Task<IActionResult> Trigger(string token, CancellationToken cancellationToken)
        {
            if (!_tokenService.TryDecryptToken(token, out Signal signal))
                return NotFound();

            var workflowInstance = await _workflowInstanceManager.GetByWorkflowInstanceIdAsync(signal.WorkflowInstanceId, cancellationToken);

            if (workflowInstance == null)
                return NotFound();

            var input = signal.Name;

            await _workflowRunner.RunWorkflowAsync(workflowInstance, input, cancellationToken: cancellationToken);

            return HttpContext.Items.ContainsKey(WorkflowHttpResult.Instance)
                ? (IActionResult)new EmptyResult()
                : Accepted();
        }
    }
}