using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Webhooks.Swagger.Examples;
using Elsa.Persistence.Specifications;
using Elsa.Services;
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
    public class Post : ControllerBase
    {
        private readonly IWebhookDefinitionStore _webhookDefinitionStore;
        private readonly IIdGenerator _idGenerator;

        public Post(IWebhookDefinitionStore store, IIdGenerator idGenerator)
        {
            _webhookDefinitionStore = store;
            _idGenerator = idGenerator;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(WebhookDefinition))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(WebhookDefinitionExample))]
        [SwaggerOperation(
            Summary = "Creates a new webhook definition or updates an existing one.",
            Description = "Creates a new webhook definition or updates an existing one.",
            OperationId = "WebhookDefinitions.Post",
            Tags = new[] { "WebhookDefinitions" })
        ]
        public async Task<ActionResult<WebhookDefinition>> Handle([FromBody] SaveWebhookDefinitionRequest request, [FromRoute] ApiVersion apiVersion, CancellationToken cancellationToken)
        {
            var webhookId = request.Id;
            var webhookDefinition = !string.IsNullOrWhiteSpace(webhookId) ? await _webhookDefinitionStore.FindAsync(new EntityIdSpecification<WebhookDefinition>(webhookId), cancellationToken) : default;

            if (webhookDefinition == null)
                webhookDefinition = new WebhookDefinition
                {
                    Id = !string.IsNullOrWhiteSpace(webhookId) ? webhookId : _idGenerator.Generate(),
                };

            webhookDefinition.Name = request.Name.Trim();
            webhookDefinition.Path = request.Path.Trim();
            webhookDefinition.Description = request.Description?.Trim();
            webhookDefinition.PayloadTypeName = request.PayloadTypeName?.Trim();
            webhookDefinition.IsEnabled = request.IsEnabled;

            await _webhookDefinitionStore.SaveAsync(webhookDefinition, cancellationToken);

            return CreatedAtAction("Handle", "Get", new { id = webhookDefinition.Id, apiVersion = apiVersion.ToString() }, webhookDefinition);
        }
    }
}