using System.Collections.Generic;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Webhooks.Models;
using Elsa.Activities.Webhooks.Persistence;
using Elsa.Models;
using Elsa.Persistence.Specifications;
using Elsa.Serialization;
using Elsa.Server.Api.Models;
using Elsa.Server.Api.Swagger.Examples;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;

namespace Elsa.Server.Api.Endpoints.WebhookDefinitions
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/webhook-definitions")]
    [Produces(MediaTypeNames.Application.Json)]
    public class List : Controller
    {
        private readonly IWebhookDefinitionStore _webhookDefinitionStore;
        private readonly IContentSerializer _serializer;

        public List(IWebhookDefinitionStore webhookDefinitionStore, IContentSerializer serializer)
        {
            _webhookDefinitionStore = webhookDefinitionStore;
            _serializer = serializer;
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
        public async Task<ActionResult<PagedList<WorkflowDefinition>>> Handle(CancellationToken cancellationToken = default)
        {
            var specification = Specification<WebhookDefinition>.All;
            var items = await _webhookDefinitionStore.FindManyAsync(specification, cancellationToken: cancellationToken);

            return Json(items, _serializer.GetSettings());
        }
    }
}