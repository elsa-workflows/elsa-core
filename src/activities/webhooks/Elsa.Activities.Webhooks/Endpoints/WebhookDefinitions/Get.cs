using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence.Specifications;
using Elsa.Server.Api.Services;
using Elsa.Server.Api.Swagger.Examples;
using Elsa.Webhooks.Abstractions.Models;
using Elsa.Webhooks.Abstractions.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;

namespace Elsa.Activities.Webhooks.Endpoints.WebhookDefinitions
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/webhook-definitions/{webhookId}")]
    [Produces(MediaTypeNames.Application.Json)]
    public class Get : Controller
    {
        private readonly IWebhookDefinitionStore _webhookDefinitionStore;
        private readonly IEndpointContentSerializerSettingsProvider _serializerSettingsProvider;

        public Get(IWebhookDefinitionStore webhookDefinitionStore, IEndpointContentSerializerSettingsProvider serializerSettingsProvider)
        {
            _webhookDefinitionStore = webhookDefinitionStore;
            _serializerSettingsProvider = serializerSettingsProvider;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WebhookDefinition))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(WebhookDefinitionExample))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerOperation(
            Summary = "Returns a single webhook definition.",
            Description = "Returns a single webhook definition using the specified webhook definition ID.",
            OperationId = "WebhookDefinitions.GetByDefinition",
            Tags = new[] { "WebhookDefinitions" })
        ]
        public async Task<IActionResult> Handle(string webhookId, CancellationToken cancellationToken = default)
        {
            var webhookDefinition = await _webhookDefinitionStore.FindAsync(new EntityIdSpecification<WebhookDefinition>(webhookId), cancellationToken);
            return webhookDefinition == null ? (IActionResult) NotFound() : Json(webhookDefinition, _serializerSettingsProvider.GetSettings());
        }
    }
}