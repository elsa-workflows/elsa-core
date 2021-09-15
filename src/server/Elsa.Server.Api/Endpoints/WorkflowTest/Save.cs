using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Server.Api.ActionFilters;
using Elsa.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NJsonSchema;
using Swashbuckle.AspNetCore.Annotations;

namespace Elsa.Server.Api.Endpoints.WorkflowTest
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/workflow-test/save")]
    [Produces("application/json")]
    public class Save : Controller
    {
        private readonly IWorkflowPublisher _workflowPublisher;
        private readonly IMediator _mediator;

        public Save(IWorkflowPublisher workflowPublisher, IMediator mediator)
        {
            _workflowPublisher = workflowPublisher;
            _mediator = mediator;
        }

        [HttpPost]
        [ElsaJsonFormatter]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WorkflowTestSaveRequest))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerOperation(
            Summary = "Updates Json Schema for the specified workflow definition version activity in test mode.",
            Description = "Updates Json Schema the specified workflow definition vesrion activity in test mode.",
            OperationId = "WorkflowTest.Save",
            Tags = new[] { "WorkflowTest" })
        ]
        public async Task<IActionResult> Handle([FromBody] WorkflowTestSaveRequest request, CancellationToken cancellationToken = default)
        {
            var workflowDefinitionId = request.WorkflowDefinitionId;
            var workflowDefinition = !string.IsNullOrWhiteSpace(workflowDefinitionId) ? await _workflowPublisher.GetAsync(workflowDefinitionId, request.Version, cancellationToken) : default;

            if (workflowDefinition == null)
                return NotFound();

            var schema = JsonSchema.FromSampleJson(JsonConvert.SerializeObject(request.Json));
            var schemaJson = schema.ToJson();

            var activity = workflowDefinition.Activities.FirstOrDefault(x => x.ActivityId == request.ActivityId);
            if (activity != null)
            {
                var property = activity.Properties.FirstOrDefault(x => x.Name == "Schema");
                if (property != null)
                {
                    property.Expressions.Remove("Literal");
                    property.Expressions.Add("Literal", schemaJson);
                }
            }

            workflowDefinition = await _workflowPublisher.SaveDraftAsync(workflowDefinition, cancellationToken);
            await _mediator.Publish(new WorkflowDefinitionUpdated(workflowDefinition), cancellationToken);

            return CreatedAtAction("Handle", "GetByVersionId", new { versionId = workflowDefinition.Id }, workflowDefinition);
        }
    }
}