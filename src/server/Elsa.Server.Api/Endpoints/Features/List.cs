using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Server.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Elsa.Server.Api.Endpoints.Features
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/features")]
    [Produces("application/json")]
    public class List : Controller
    {
        private readonly IEnumerable<IServerFeatureProvider> _providers;

        public List(IEnumerable<IServerFeatureProvider> providers)
        {
            _providers = providers;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [SwaggerOperation(
            Summary = "Returns a list of features supported by this server.",
            Description = "Returns a list of features supported by this server.",
            OperationId = "Features.Get",
            Tags = new[] { "Features" })
        ]
        public async Task<IActionResult> Handle(CancellationToken cancellationToken)
        {
            var features = await GetFeaturesAsync(cancellationToken).ToListAsync(cancellationToken);

            var model = new
            {
                Features = features
            };

            return Ok(model);
        }

        private async IAsyncEnumerable<string> GetFeaturesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var provider in _providers)
            {
                var features = await provider.GetFeaturesAsync(cancellationToken);

                foreach (var feature in features)
                    yield return feature;
            }
        }
    }
}