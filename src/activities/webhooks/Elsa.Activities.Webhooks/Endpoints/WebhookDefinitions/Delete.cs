using System.Threading;
using System.Threading.Tasks;
using Elsa.Persistence.Specifications;
using Elsa.Webhooks.Abstractions.Models;
using Elsa.Webhooks.Abstractions.Persistence;
using Elsa.Webhooks.Abstractions.Services;
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
        private readonly IWebhookPublisher _webhookPublisher;

        public Delete(IWebhookDefinitionStore webhookDefinitionStore, IWebhookPublisher webhookPublisher)
        {
            _webhookDefinitionStore = webhookDefinitionStore;
            _webhookPublisher = webhookPublisher;
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
            await _webhookPublisher.DeleteAsync(id, cancellationToken);
            //var webhookDefinition = !string.IsNullOrWhiteSpace(id) ? await _webhookDefinitionStore.FindAsync(new EntityIdSpecification<WebhookDefinition>(id), cancellationToken) : default;

            //if (webhookDefinition != null)
            //{
            //    await _webhookPublisher.DeleteAsync(webhookDefinition, cancellationToken);
            //}

            return Accepted();
        }
    }
}