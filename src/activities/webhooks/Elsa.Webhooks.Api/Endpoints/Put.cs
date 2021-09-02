using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Webhooks.Swagger.Examples;
using Elsa.Persistence.Specifications;
using Elsa.Webhooks.Api.Models;
using Elsa.Webhooks.Models;
using Elsa.Webhooks.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;

namespace Elsa.Webhooks.Api.Endpoints
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/webhook-definitions")]
    [Produces("application/json")]
    public class Update : ControllerBase
    {
        private readonly IWebhookDefinitionStore _webhookDefinitionStore;
        public Update(IWebhookDefinitionStore store) => _webhookDefinitionStore = store;

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
            var webhookDefinition = await _webhookDefinitionStore.FindAsync(new EntityIdSpecification<WebhookDefinition>(webhookId), cancellationToken);

            if (webhookDefinition == null)
                return NotFound();

            webhookDefinition.Name = request.Name.Trim();
            webhookDefinition.Path = request.Path.Trim();
            webhookDefinition.Description = request.Description?.Trim();
            webhookDefinition.PayloadTypeName = request.PayloadTypeName?.Trim();
            webhookDefinition.IsEnabled = request.IsEnabled;

            await _webhookDefinitionStore.UpdateAsync(webhookDefinition, cancellationToken);

            return Ok(webhookDefinition);
        }
    }
}