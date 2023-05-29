using System.Threading;
using System.Threading.Tasks;
using Elsa.Server.Api.ActionFilters;
using Elsa.Services;
using Elsa.WorkflowTesting.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Elsa.WorkflowTesting.Api.Endpoints
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/workflow-test/execute")]
    [Produces("application/json")]
    public class Execute : Controller
    {
        private readonly IWorkflowLaunchpad _workflowLaunchpad;
        private readonly ITenantAccessor _tenantAccessor;

        public Execute(IWorkflowLaunchpad workflowLaunchpad, ITenantAccessor tenantAccessor)
        {
            _workflowLaunchpad = workflowLaunchpad;
            _tenantAccessor = tenantAccessor;
        }

        [HttpPost]
        [ElsaJsonFormatter]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WorkflowTestExecuteResponse))]
        [SwaggerOperation(
            Summary = "Executes the specified workflow definition version in test mode.",
            Description = "Executes the specified workflow definition version in test mode.",
            OperationId = "WorkflowTest.Execute",
            Tags = new[] { "WorkflowTest" })
        ]
        public async Task<IActionResult> Handle([FromBody] WorkflowTestExecuteRequest request, CancellationToken cancellationToken = default)
        {
            var tenantId = await _tenantAccessor.GetTenantIdAsync(cancellationToken);
            var testCorrelation = "test-" + request.WorkflowDefinitionId;
            var startableWorkflow = await _workflowLaunchpad.FindStartableWorkflowAsync(request.WorkflowDefinitionId, request.Version, request.StartActivityId, testCorrelation, default, tenantId, cancellationToken);

            if (startableWorkflow == null)
                return Ok(new WorkflowTestExecuteResponse { IsSuccess = false, IsAnotherInstanceRunning = true });

            startableWorkflow.WorkflowInstance.SetMetadata("isTest", true);
            startableWorkflow.WorkflowInstance.SetMetadata("signalRConnectionId", request.SignalRConnectionId);

            await _workflowLaunchpad.ExecuteStartableWorkflowAsync(startableWorkflow, default, cancellationToken);

            if (Response.HasStarted)
                return new EmptyResult();

            return Ok(new WorkflowTestExecuteResponse { IsSuccess = true });
        }
    }
}