using System.Threading;
using System.Threading.Tasks;
using Elsa.Secrets.Extensions;
using Elsa.Secrets.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Secrets.Api.Endpoints.Secrets
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/secrets/{id}")]
    [Produces("application/json")]
    public class Delete : Controller
    {
        private readonly ISecretsStore _secretsStore;

        public Delete(ISecretsStore secretsStore)
        { 
            _secretsStore = secretsStore;
        }

        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Handle(string id, CancellationToken cancellationToken = default)
        {
            var secret = await _secretsStore.FindByIdAsync(id, cancellationToken);

            if (secret == null)
                return NotFound();

            await _secretsStore.DeleteAsync(secret, cancellationToken);
            return NoContent();
        }
    }
}
