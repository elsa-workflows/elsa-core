using System.Collections.Generic;
using System.Linq;
using Elsa.MultiTenancy;
using Elsa.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Elsa.Server.Api.Endpoints.Tenants
{
    [ApiController]
    [ApiVersion("1")]
    [Route("{tenant}/v{version:apiVersion}/tenants/listPrefixes")]
    [Route("v{version:apiVersion}/tenants/listPrefixes")]
    [Produces("application/json")]
    public class ListPrefixes : Controller
    {
        private readonly ITenantStore _tenantStore;
        private readonly IContentSerializer _contentSerializer;

        public ListPrefixes(ITenantStore tenantStore, IContentSerializer contentSerializer)
        {
            _tenantStore = tenantStore;
            _contentSerializer = contentSerializer;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<string>))]
        [SwaggerOperation(
            Summary = "Returns prefixes for all tenants",
            Description = "Returns prefixes for all tenants found in configuration.",
            OperationId = "Tenants.ListPrefixes",
            Tags = new[] { "Tenants" })
        ]
        public IActionResult Handle()
        {
            var prefixes = _tenantStore.GetTenants().Where(x => !x.IsDefault && !string.IsNullOrEmpty(x.Prefix)).Select(x => x.Prefix).ToList();

            return Json(prefixes, _contentSerializer.GetSettings());
        }
    }
}
