using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Webhooks.Swagger.Examples;
using Elsa.Persistence.Specifications;
using Elsa.Webhooks.Models;
using Elsa.Webhooks.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;

namespace Elsa.Activities.Webhooks.Endpoints.WebhookDefinitions
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/webhook-definitions")]
    [Produces("application/json")]
    public class Update : ControllerBase
    {
        private readonly IWebhookDefinitionStore _store;
        public Update(IWebhookDefinitionStore store) => _store = store;

        [HttpPut]
        [ProducesResponseType(typeof(WebhookDefinition), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(WebhookDefinitionExample))]
        [SwaggerOperation(
            Summary = "Updates an existing webhook.",
            Description = "Updates an existing webhook definition.",
            OperationId = "WebhookDefinitions.Put",
            Tags = new[] { "WebhookDefinitions" })
        ]
        public async Task<ActionResult<WebhookDefinition>> Handle([FromBody] SaveWebhookDefinitionRequest request, [FromRoute] ApiVersion apiVersion, CancellationToken cancellationToken)
        {
            var webhookId = request.Id;
            var webhookDefinition = await _store.FindAsync(new EntityIdSpecification<WebhookDefinition>(webhookId), cancellationToken);

            if (webhookDefinition == null)
                return NotFound();

            webhookDefinition.Name = request.Name.Trim();
            webhookDefinition.Path = request.Path.Trim();
            webhookDefinition.Description = request.Description?.Trim();
            webhookDefinition.PayloadTypeName = request.PayloadTypeName?.Trim();
            webhookDefinition.IsEnabled = request.IsEnabled;

            await _store.UpdateAsync(webhookDefinition, cancellationToken);
            
            return Ok(webhookDefinition);
        }
    }
}