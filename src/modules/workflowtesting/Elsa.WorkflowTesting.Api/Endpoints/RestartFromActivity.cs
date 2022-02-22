using System.Threading;
using System.Threading.Tasks;
using Elsa.Server.Api.ActionFilters;
using Elsa.Services;
using Elsa.WorkflowTesting.Api.Models;
using Elsa.WorkflowTesting.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Elsa.WorkflowTesting.Api.Endpoints
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/workflow-test/restartFromActivity")]
    [Produces("application/json")]
    public class RestartFromActivity : Controller
    {
        private readonly IWorkflowTestLaunchpad _workflowTestLaunchpad;
        private readonly ITenantAccessor _tenantAccessor;

        public RestartFromActivity(IWorkflowTestLaunchpad workflowTestLaunchpad, ITenantAccessor tenantAccessor)
        {
            _workflowTestLaunchpad = workflowTestLaunchpad;
            _tenantAccessor = tenantAccessor;
        }

        [HttpPost]
        [ElsaJsonFormatter]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WorkflowTestRestartFromActivityRequest))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerOperation(
            Summary = "Executes the specified workflow definition version in test mode.",
            Description = "Executes the specified workflow definition version in test mode.",
            OperationId = "WorkflowTest.Restart",
            Tags = new[] { "WorkflowTest" })
        ]
        public async Task<IActionResult> Handle([FromBody] WorkflowTestRestartFromActivityRequest request, CancellationToken cancellationToken = default)
        {
            var tenantId = await _tenantAccessor.GetTenantIdAsync(cancellationToken);
            var result = await _workflowTestLaunchpad.FindAndRestartTestWorkflowAsync(request.WorkflowDefinitionId, request.ActivityId, request.Version, request.SignalRConnectionId, request.LastWorkflowInstanceId, tenantId, cancellationToken);

            if (result == null)
                return NotFound();

            if (Response.HasStarted)
                return new EmptyResult();

            return Ok();
        }
    }
}