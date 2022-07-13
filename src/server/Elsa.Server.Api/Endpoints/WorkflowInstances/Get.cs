using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Serialization;
using Elsa.Server.Core.Events;
using Elsa.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Elsa.Server.Api.Endpoints.WorkflowInstances
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/workflow-instances/{id}")]
    [Produces("application/json")]
    public class Get : Controller
    {
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly IContentSerializer _contentSerializer;
        private readonly IWorkflowRegistry _workflowRegistry;
        private readonly ITenantAccessor _tenantAccessor;
        private readonly IPublisher _publisher;

        public Get(IWorkflowInstanceStore workflowInstanceStore, IContentSerializer contentSerializer, IWorkflowRegistry workflowRegistry, ITenantAccessor tenantAccessor, IPublisher publisher)
        {
            _workflowInstanceStore = workflowInstanceStore;
            _contentSerializer = contentSerializer;
            _workflowRegistry = workflowRegistry;
            _tenantAccessor = tenantAccessor;
            _publisher = publisher;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WorkflowInstance))]
        [SwaggerOperation(
            Summary = "Returns a single workflow instance.",
            Description = "Returns a single workflow instance.",
            OperationId = "WorkflowInstances.Get",
            Tags = new[] { "WorkflowInstances" })
        ]
        public async Task<IActionResult> Handle(string id, CancellationToken cancellationToken = default)
        {
            var workflowInstance = await _workflowInstanceStore.FindByIdAsync(id, cancellationToken);

            if (workflowInstance == null)
                return NotFound();

            var tenantId = await _tenantAccessor.GetTenantIdAsync(cancellationToken);
            var workflowBlueprint = await _workflowRegistry.FindByDefinitionVersionIdAsync(workflowInstance.DefinitionVersionId, tenantId, cancellationToken);
            await _publisher.Publish(new RequestingWorkflowInstance(workflowInstance, workflowBlueprint!), cancellationToken);

            return Json(workflowInstance, _contentSerializer.GetSettings());
        }
    }
}