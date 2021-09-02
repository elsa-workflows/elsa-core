using System.Collections.Generic;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Webhooks.Swagger.Examples;
using Elsa.Persistence.Specifications;
using Elsa.Server.Api.Models;
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
    [Route("v{apiVersion:apiVersion}/webhook-definitions")]
    [Produces(MediaTypeNames.Application.Json)]
    public class List : Controller
    {
        private readonly IWebhookDefinitionStore _webhookDefinitionStore;
        private readonly IEndpointContentSerializerSettingsProvider _serializerSettingsProvider;

        public List(IWebhookDefinitionStore store, IEndpointContentSerializerSettingsProvider serializerSettingsProvider)
        {
            _webhookDefinitionStore = store;
            _serializerSettingsProvider = serializerSettingsProvider;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<WebhookDefinition>))]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(WebhookDefinitionListExample))]
        [SwaggerOperation(
            Summary = "Returns a list of webhook definitions.",
            Description = "Returns a list of webhook definitions.",
            OperationId = "WebhookDefinitions.List",
            Tags = new[] { "WebhookDefinitions" })
        ]
        public async Task<ActionResult<PagedList<WebhookDefinition>>> Handle(CancellationToken cancellationToken = default)
        {
            var specification = Specification<WebhookDefinition>.Identity;
            var items = await _webhookDefinitionStore.FindManyAsync(specification, cancellationToken: cancellationToken);

            return Json(items, _serializerSettingsProvider.GetSettings());
        }
    }
}