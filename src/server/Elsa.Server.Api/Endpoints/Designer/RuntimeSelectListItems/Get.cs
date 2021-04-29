using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Design;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Annotations;

namespace Elsa.Server.Api.Endpoints.Designer.RuntimeSelectListItems
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/designer/runtime-select-list-items/{providerTypeName}")]
    [Produces("application/json")]
    public class Get : Controller
    {
        private readonly IServiceProvider _serviceProvider;

        public Get(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OkObjectResult))]
        [SwaggerOperation(
            Summary = "Returns a list of items to be used by the list control requesting the items.",
            Description = "Returns a list of items to be used by the list control requesting the items.",
            OperationId = "Designer.RuntimeSelectListItems.Get",
            Tags = new[] { "Designer.RuntimeSelectListItems" })
        ]
        public async Task<IActionResult> Handle(string providerTypeName, object? context = default, CancellationToken cancellationToken = default)
        {
            var type = Type.GetType(providerTypeName)!;
            var provider = (IRuntimeSelectListItemsProvider)ActivatorUtilities.GetServiceOrCreateInstance(_serviceProvider, type);
            var items = (await provider.GetItemsAsync(context, cancellationToken)).ToList();

            return Ok(items);
        }
    }
}