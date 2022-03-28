using Elsa.Secrets.Api.Models;
using Elsa.Secrets.Models;
using Elsa.Secrets.Persistence;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Secrets.Api.Endpoints
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/secrets")]
    [Produces("application/json")]
    public class Save : Controller
    {
        private readonly ISecretsStore _secretsStore;
        public Save(ISecretsStore secretsStore)
        {
            _secretsStore = secretsStore;
        }

        [HttpPost]
        public async Task<ActionResult<Secret>> Handle([FromBody] SaveSecretRequet request, [FromRoute] ApiVersion apiVersion, CancellationToken cancellationToken)
        {
            var model = new Secret
            {
                DisplayName = request.DisplayName,
                Name = request.Name,
                Type = request.Type,   
                PropertiesJson = JsonConvert.SerializeObject(request.Properties)
            };

            await _secretsStore.SaveAsync(model);

            return Ok(model);

        }
    }
}
