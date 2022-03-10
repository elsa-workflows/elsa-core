using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Design;
using Elsa.Server.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;

namespace Elsa.Server.Api.Endpoints.Designer.RuntimeSelectListItems
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/designer/runtime-select-list")]
    [Produces("application/json")]
    public class Get : Controller
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IEndpointContentSerializerSettingsProvider _serializerSettingsProvider;

        public Get(IServiceProvider serviceProvider, IEndpointContentSerializerSettingsProvider serializerSettingsProvider)
        {
            _serviceProvider = serviceProvider;
            _serializerSettingsProvider = serializerSettingsProvider;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OkObjectResult))]
        [SwaggerOperation(
            Summary = "Returns a list of items to be used by the list control requesting the items.",
            Description = "Returns a list of items to be used by the list control requesting the items.",
            OperationId = "Designer.RuntimeSelectListItems.Get",
            Tags = new[] { "Designer.RuntimeSelectListItems" })
        ]
        public async Task<IActionResult> Handle([FromBody] RuntimeSelectListContextHolder model, CancellationToken cancellationToken = default)
        {
            var type = Type.GetType(model.ProviderTypeName)!;
            var provider = ActivatorUtilities.GetServiceOrCreateInstance(_serviceProvider, type);
            var context = model.Context;
            var serializerSettings = _serializerSettingsProvider.GetSettings();
            var selectList = await GetSelectList(provider, context, cancellationToken);

            return Json(selectList, serializerSettings);
        }

        private async Task<SelectList> GetSelectList(object provider, object? context, CancellationToken cancellationToken)
        {
            if (provider is IRuntimeSelectListProvider newProvider)
                return await newProvider.GetSelectListAsync(context, cancellationToken);

#pragma warning disable CS0618
            var items = await ((IRuntimeSelectListItemsProvider)provider).GetItemsAsync(context, cancellationToken);
#pragma warning restore CS0618

            return new SelectList
            {
                Items = items.ToList()
            };
        }
    }

    [JsonObject(ItemTypeNameHandling = TypeNameHandling.All)]
    public class RuntimeSelectListContextHolder
    {
        public string ProviderTypeName { get; set; } = default!;

        public object? Context { get; set; }
    };
}