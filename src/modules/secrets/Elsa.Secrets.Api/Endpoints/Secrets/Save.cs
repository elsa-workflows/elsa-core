using System.Threading;
using System.Threading.Tasks;
using Elsa.Secrets.Api.Models;
using Elsa.Secrets.Models;
using Elsa.Secrets.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Secrets.Api.Endpoints.Secrets
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
                Id = request.SecretId,
                DisplayName = request.Name,
                Name = request.Name,
                Type = request.Type,   
                Properties = request.Properties
            };

            if (model.Id == null)
                await _secretsStore.AddAsync(model);
            else
                await _secretsStore.UpdateAsync(model);

            return Ok(model);
        }
    }
}
