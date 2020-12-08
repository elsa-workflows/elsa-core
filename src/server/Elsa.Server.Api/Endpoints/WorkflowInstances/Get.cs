using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Server.Api.Endpoints.WorkflowRegistry;
using Elsa.Server.Api.Models;
using Elsa.Server.Api.Swagger.Examples;
using Elsa.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;

namespace Elsa.Server.Api.Endpoints.WorkflowInstances
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/workflow-instances/{id}")]
    [Produces("application/json")]
    public class Get : Controller
    {
        private readonly IWorkflowInstanceManager _workflowInstanceManager;

        public Get(IWorkflowInstanceManager workflowInstanceManager)
        {
            _workflowInstanceManager = workflowInstanceManager;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WorkflowInstance))]
        [SwaggerOperation(
            Summary = "Returns a single workflow instance.",
            Description = "Returns a single workflow instance.",
            OperationId = "WorkflowInstances.Get",
            Tags = new[] { "WorkflowInstances" })
        ]
        public async Task<ActionResult<WorkflowInstance>> Handle(string id, CancellationToken cancellationToken = default)
        {
            var workflowInstance = await _workflowInstanceManager.GetByIdAsync(id, cancellationToken);
            return workflowInstance ?? (ActionResult<WorkflowInstance>) NotFound();
        }
    }
}