using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Secrets.Api.Models;
using Elsa.Secrets.Manager;
using Elsa.Secrets.Models;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Secrets.Api.Endpoints.Secrets
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/secrets")]
    [Produces("application/json")]
    public class Save : Controller
    {
        private readonly ISecretsManager _secretsManager;
        public Save(ISecretsManager secretsManager)
        {
            _secretsManager = secretsManager;
        }

        [HttpPost]
        public async Task<ActionResult<Secret>> Handle([FromBody] SaveSecretRequet request, [FromRoute] ApiVersion apiVersion, CancellationToken cancellationToken)
        {
            var model = new Secret
            {
                Id = request.SecretId,
                DisplayName = request.Name,
                Name = request.Name,
                Type = request.Type,
                Properties = request.Properties
            };
            model = await _secretsManager.AddOrUpdateSecret(model, true, cancellationToken);

            return Ok(model);
        }
    }
}
