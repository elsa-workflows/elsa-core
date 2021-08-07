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
    [Route("v{apiVersion:apiVersion}/workflows/{workflowDefinitionId}/execute")]
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
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ExecuteWorkflowDefinitionResponseModel))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerOperation(
            Summary = "Executes the specified workflow definition.",
            Description = "Executes the specified workflow definition.",
            OperationId = "WorkflowDefinitions.Execute",
            Tags = new[] { "WorkflowDefinitions" })
        ]
        public async Task<IActionResult> Handle(string workflowDefinitionId, ExecuteWorkflowDefinitionRequestModel request, CancellationToken cancellationToken = default)
        {
            var tenantId = await _tenantAccessor.GetTenantIdAsync(cancellationToken);
            var startableWorkflow = await _workflowLaunchpad.FindStartableWorkflowAsync(workflowDefinitionId, request.ActivityId, request.CorrelationId, request.ContextId, tenantId, cancellationToken);

            if (startableWorkflow == null)
                return NotFound();

            var result = await _workflowLaunchpad.ExecuteStartableWorkflowAsync(startableWorkflow, new WorkflowInput(request.Input), cancellationToken);

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