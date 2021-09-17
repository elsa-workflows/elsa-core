using System.Threading;
using System.Threading.Tasks;
using Elsa.Persistence.Specifications;
using Elsa.Webhooks.Models;
using Elsa.Webhooks.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Elsa.Webhooks.Api.Endpoints
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/webhook-definitions/{id}")]
    [Produces("application/json")]
    public class Delete : ControllerBase
    {
        private readonly IWebhookDefinitionStore _webhookDefinitionStore;
        public Delete(IWebhookDefinitionStore store) => _webhookDefinitionStore = store;

        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerOperation(
            Summary = "Deletes a webhook definition.",
            Description = "Deletes a webhook definition.",
            OperationId = "WebhookDefinitions.Delete",
            Tags = new[] { "WebhookDefinitions" })
        ]
        public async Task<IActionResult> Handle(string id, CancellationToken cancellationToken)
        {
            var webhookDefinition = await _webhookDefinitionStore.FindAsync(new EntityIdSpecification<WebhookDefinition>(id), cancellationToken);

            if (webhookDefinition == null)
                return NotFound();

            await _webhookDefinitionStore.DeleteAsync(webhookDefinition, cancellationToken);
            return Ok();
        }
    }
}