using Elsa.Server.Api.ActionFilters;
using Elsa.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.Server.Api.Endpoints.Workflows
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/workflows/{workflowDefinitionId}/{testId}/test")]
    [Produces("application/json")]
    public class Test : Controller
    {
        private readonly IWorkflowLaunchpad _workflowLaunchpad;
        private readonly ITenantAccessor _tenantAccessor;

        public Test(IWorkflowLaunchpad workflowLaunchpad, ITenantAccessor tenantAccessor)
        {
            _workflowLaunchpad = workflowLaunchpad;
            _tenantAccessor = tenantAccessor;
        }

        [HttpPost]
        [ElsaJsonFormatter]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ExecuteWorkflowDefinitionResponseModel))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerOperation(
            Summary = "Executes the specified workflow definition in test mode.",
            Description = "Executes the specified workflow definition in test mode.",
            OperationId = "WorkflowDefinitions.Test",
            Tags = new[] { "WorkflowDefinitions" })
        ]
        public async Task<IActionResult> Handle(string workflowDefinitionId, string testId, ExecuteWorkflowDefinitionRequestModel request, CancellationToken cancellationToken = default)
        {
            var tenantId = await _tenantAccessor.GetTenantIdAsync(cancellationToken);
            var startableWorkflow = await _workflowLaunchpad.FindTestableWorkflowAsync(workflowDefinitionId, request.ActivityId, request.CorrelationId, request.ContextId, tenantId, cancellationToken);

            if (startableWorkflow == null)
                return NotFound();

            var result = await _workflowLaunchpad.ExecuteTestableWorkflowAsync(startableWorkflow, new WorkflowInput(request.Input), cancellationToken);

            if (Response.HasStarted)
                return new EmptyResult();

            return Ok(new ExecuteWorkflowDefinitionResponseModel(
                result.Executed,
                result.ActivityId,
                result.WorkflowInstance
            ));
        }
    }
}