using System.Threading;
using System.Threading.Tasks;
using Elsa.Server.Api.Swagger.Examples;
using Elsa.Webhooks.Abstractions.Models;
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
    public partial class Save : ControllerBase
    {
        private readonly IWebhookPublisher _webhookPublisher;

        public Save(IWebhookPublisher webhookPublisher)
        {
            _webhookPublisher = webhookPublisher;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(WebhookDefinition))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(WebhookDefinitionExample))]
        [SwaggerOperation(
            Summary = "Creates a new webhook definition or updates an existing one.",
            Description =
                "Creates a new webhook definition or updates an existing one.",
            OperationId = "WebhookDefinitions.Save",
            Tags = new[] { "WebhookDefinitions" })
        ]
        public async Task<ActionResult<WebhookDefinition>> Handle([FromBody] SaveRequest request, [FromRoute] ApiVersion apiVersion, CancellationToken cancellationToken)
        {
            var webhookDefinitionId = request.WebhookDefinitionId;
            var webhookDefinition = !string.IsNullOrWhiteSpace(webhookDefinitionId) ? await _webhookPublisher.GetAsync(webhookDefinitionId, cancellationToken) : default;

            if (webhookDefinition == null)
            {
                webhookDefinition = _webhookPublisher.New();

                if (!string.IsNullOrWhiteSpace(webhookDefinitionId))
                    webhookDefinition.DefinitionId = webhookDefinitionId;
            }

            webhookDefinition.Description = request.Description?.Trim();
            webhookDefinition.Name = request.Name?.Trim();
            webhookDefinition.IsEnabled = request.IsEnabled;

            webhookDefinition = await _webhookPublisher.SaveAsync(webhookDefinition, cancellationToken);

            return CreatedAtAction("Handle", "Get", new { definitionId = webhookDefinition.DefinitionId, apiVersion = apiVersion.ToString() }, webhookDefinition);
        }
    }
}