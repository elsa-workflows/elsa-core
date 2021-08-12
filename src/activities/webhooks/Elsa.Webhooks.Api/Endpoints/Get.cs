using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Webhooks.Swagger.Examples;
using Elsa.Persistence.Specifications;
using Elsa.Server.Api.Services;
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
    [Route("v{apiVersion:apiVersion}/webhook-definitions/{id}")]
    [Produces(MediaTypeNames.Application.Json)]
    public class Get : Controller
    {
        private readonly IWebhookDefinitionStore _webhookDefinitionStore;
        private readonly IEndpointContentSerializerSettingsProvider _serializerSettingsProvider;

        public Get(IWebhookDefinitionStore store, IEndpointContentSerializerSettingsProvider serializerSettingsProvider)
        {
            _webhookDefinitionStore = store;
            _serializerSettingsProvider = serializerSettingsProvider;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WebhookDefinition))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(WebhookDefinitionExample))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerOperation(
            Summary = "Returns a single webhook definition.",
            Description = "Returns a single webhook definition using the specified webhook definition ID.",
            OperationId = "WebhookDefinitions.Get",
            Tags = new[] { "WebhookDefinitions" })
        ]
        public async Task<IActionResult> Handle(string id, CancellationToken cancellationToken = default)
        {
            var webhookDefinition = await _webhookDefinitionStore.FindAsync(new EntityIdSpecification<WebhookDefinition>(id), cancellationToken);
            return webhookDefinition == null ? NotFound() : Json(webhookDefinition, _serializerSettingsProvider.GetSettings());
        }
    }
}