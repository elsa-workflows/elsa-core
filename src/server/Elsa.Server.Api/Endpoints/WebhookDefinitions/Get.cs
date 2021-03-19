using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Webhooks.Models;
using Elsa.Activities.Webhooks.Persistence;
using Elsa.Models;
using Elsa.Persistence.Specifications;
using Elsa.Serialization;
using Elsa.Server.Api.Swagger.Examples;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;

namespace Elsa.Server.Api.Endpoints.WebhookDefinitions
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/webhook-definitions/{webhookDefinitionId}")]
    [Produces(MediaTypeNames.Application.Json)]
    public class Get : Controller
    {
        private readonly IWebhookDefinitionStore _webhookDefinitionStore;
        private readonly IContentSerializer _serializer;

        public Get(IWebhookDefinitionStore webhookDefinitionStore, IContentSerializer serializer)
        {
            _webhookDefinitionStore = webhookDefinitionStore;
            _serializer = serializer;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WorkflowDefinition))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(WorkflowDefinitionExample))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SwaggerOperation(
            Summary = "Returns a single webhook definition.",
            Description = "Returns a single webhook definition using the specified webhook definition ID.",
            OperationId = "WebhookDefinitions.GetByDefinition",
            Tags = new[] { "WebhookDefinitions" })
        ]
        public async Task<IActionResult> Handle(string webhookDefinitionId, CancellationToken cancellationToken = default)
        {
            var webhookDefinition = await _webhookDefinitionStore.FindAsync(new EntityIdSpecification<WebhookDefinition>(webhookDefinitionId), cancellationToken);
            return webhookDefinition == null ? (IActionResult) NotFound() : Json(webhookDefinition, _serializer.GetSettings());
        }
    }
}