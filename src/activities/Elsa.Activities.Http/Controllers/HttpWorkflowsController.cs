using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Models;
using Elsa.Activities.Http.Services;
using Elsa.Queries;
using Elsa.Services;
using Microsoft.AspNetCore.Mvc;
using YesSql;

namespace Elsa.Activities.Http.Controllers
{
    [ApiController]
    [Route("workflows")]
    public class HttpWorkflowsController : ControllerBase
    {
        private readonly ITokenService _tokenService;
        private readonly IWorkflowHost _workflowHost;
        private readonly ISession _session;

        public HttpWorkflowsController(
            ITokenService tokenService,
            IWorkflowHost workflowHost,
            ISession session)
        {
            _tokenService = tokenService;
            _workflowHost = workflowHost;
            _session = session;
        }

        [Route("trigger/{token}")]
        [HttpGet, HttpPost]
        public async Task<IActionResult> Trigger(string token, CancellationToken cancellationToken)
        {
            if (!_tokenService.TryDecryptToken(token, out Signal signal))
                return NotFound();

            var workflowInstance = await _session.GetWorkflowInstanceByIdAsync(signal.WorkflowInstanceId, cancellationToken);

            if (workflowInstance == null)
                return NotFound();

            var input = signal.Name;

            await _workflowHost.RunWorkflowInstanceAsync(workflowInstance, input, cancellationToken: cancellationToken);

            return HttpContext.Items.ContainsKey(WorkflowHttpResult.Instance)
                ? (IActionResult)new EmptyResult()
                : Accepted();
        }
    }
}