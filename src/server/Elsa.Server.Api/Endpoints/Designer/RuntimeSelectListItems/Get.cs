using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Design;
using Elsa.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;

namespace Elsa.Server.Api.Endpoints.Designer.RuntimeSelectListItems
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/designer/runtime-select-list-items")]
    [Produces("application/json")]
    public class Get : Controller
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IContentSerializer _contentSerializer;

        public Get(IServiceProvider serviceProvider, IContentSerializer contentSerializer)
        {
            _serviceProvider = serviceProvider;
            _contentSerializer = contentSerializer;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OkObjectResult))]
        [SwaggerOperation(
            Summary = "Returns a list of items to be used by the list control requesting the items.",
            Description = "Returns a list of items to be used by the list control requesting the items.",
            OperationId = "Designer.RuntimeSelectListItems.Get",
            Tags = new[] { "Designer.RuntimeSelectListItems" })
        ]
        public async Task<IActionResult> Handle([FromBody]RuntimeSelectListItemsContextHolder model, CancellationToken cancellationToken = default)
        {
            var type = Type.GetType(model.ProviderTypeName)!;
            var provider = (IRuntimeSelectListItemsProvider) ActivatorUtilities.GetServiceOrCreateInstance(_serviceProvider, type);
            var context = model?.Context;
            var items = (await provider.GetItemsAsync(context, cancellationToken)).ToList();
            var serializerSettings = _contentSerializer.GetSettings();

            return Json(items, serializerSettings);
        }
    }

    [JsonObject(ItemTypeNameHandling = TypeNameHandling.All)]
    public record RuntimeSelectListItemsContextHolder
    {
        public string ProviderTypeName { get; set; } = default!;
        
        public object? Context { get; set; }
    };
}