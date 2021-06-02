using System.Threading;
using System.Threading.Tasks;
using Elsa.Persistence.Specifications;
using Elsa.Webhooks.Abstractions.Models;
using Elsa.Webhooks.Abstractions.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Elsa.Activities.Webhooks.Endpoints.WebhookDefinitions
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/webhook-definitions/{id}")]
    [Produces("application/json")]
    public class Delete : ControllerBase
    {
        private readonly IWebhookDefinitionStore _webhookDefinitionStore;

        public Delete(IWebhookDefinitionStore webhookDefinitionStore)
        {
            _webhookDefinitionStore = webhookDefinitionStore;
        }

        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [SwaggerOperation(
            Summary = "Deletes a webhook definition.",
            Description = "Deletes a webhook definition.",
            OperationId = "WebhookDefinitions.Delete",
            Tags = new[] { "WebhookDefinitions" })
        ]
        public async Task<IActionResult> Handle(string id, CancellationToken cancellationToken)
        {
            var webhookDefinition = !string.IsNullOrWhiteSpace(id) ? await _webhookDefinitionStore.FindAsync(new EntityIdSpecification<WebhookDefinition>(id), cancellationToken) : default;

            if (webhookDefinition != null)
            {
                await _webhookDefinitionStore.DeleteAsync(webhookDefinition, cancellationToken);
            }

            return Accepted();
        }
    }
}