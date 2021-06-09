using System.Threading;
using System.Threading.Tasks;
using Elsa.Persistence.Specifications;
using Elsa.Server.Api.Swagger.Examples;
using Elsa.Webhooks.Abstractions.Models;
using Elsa.Webhooks.Abstractions.Persistence;
using Elsa.Webhooks.Abstractions.Services;
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
    public partial class Update : ControllerBase
    {
        private readonly IWebhookDefinitionStore _webhookDefinitionStore;
        private readonly IWebhookPublisher _webhookPublisher;

        public Update(IWebhookDefinitionStore webhookDefinitionStore, IWebhookPublisher webhookPublisher)
        {
            _webhookDefinitionStore = webhookDefinitionStore;
            _webhookPublisher = webhookPublisher;
        }

        [HttpPut]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(WebhookDefinition))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(WebhookDefinitionExample))]
        [SwaggerOperation(
            Summary = "Updates an existing webhook.",
            Description =
                "Creates a new webhook definition or updates an existing one.",
            OperationId = "WebhookDefinitions.Put",
            Tags = new[] { "WebhookDefinitions" })
        ]
        public async Task<ActionResult<WebhookDefinition>> Handle([FromBody] SaveRequest request, [FromRoute] ApiVersion apiVersion, CancellationToken cancellationToken)
        {
            var webhookId = request.Id;
            var webhookDefinition = !string.IsNullOrWhiteSpace(webhookId) ? await _webhookDefinitionStore.FindAsync(new EntityIdSpecification<WebhookDefinition>(webhookId), cancellationToken) : default;

            webhookDefinition.Name = request.Name?.Trim();
            webhookDefinition.Path = request.Path?.Trim();
            webhookDefinition.Description = request.Description?.Trim();
            webhookDefinition.PayloadTypeName = request.PayloadTypeName?.Trim();
            webhookDefinition.IsEnabled = request.IsEnabled;

            webhookDefinition = await _webhookPublisher.UpdateAsync(webhookDefinition, cancellationToken);

            return CreatedAtAction("Handle", "Get", new { id = webhookDefinition.Id, apiVersion = apiVersion.ToString() }, webhookDefinition);
        }
    }
}