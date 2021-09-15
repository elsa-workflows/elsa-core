using Elsa.Server.Api.ActionFilters;
using Elsa.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Server.Api.Endpoints.Workflows;

namespace Elsa.Server.Api.Endpoints.WorkflowTest
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
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WorkflowTestExecuteRequest))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerOperation(
            Summary = "Executes the specified workflow definition version in test mode.",
            Description = "Executes the specified workflow definition vesrion in test mode.",
            OperationId = "WorkflowTest.Execute",
            Tags = new[] { "WorkflowTest" })
        ]
        public async Task<IActionResult> Handle([FromBody] WorkflowTestExecuteRequest request, CancellationToken cancellationToken = default)
        {
            var tenantId = await _tenantAccessor.GetTenantIdAsync(cancellationToken);
            var testableWorkflow = await _workflowLaunchpad.FindTestableWorkflowAsync(request.WorkflowDefinitionId!, request.Version, null, null, null, tenantId, request.SignalRConnectionId, cancellationToken);

            if (testableWorkflow == null)
                return NotFound();

            var result = await _workflowLaunchpad.ExecuteTestableWorkflowAsync(testableWorkflow, null, cancellationToken);

            if (Response.HasStarted)
                return new EmptyResult();

            return Ok();
        }
    }
}