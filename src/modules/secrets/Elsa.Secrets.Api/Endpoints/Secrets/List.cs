using System.Collections.Generic;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Persistence.Specifications;
using Elsa.Secrets.Manager;
using Elsa.Secrets.Models;
using Elsa.Secrets.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Secrets.Api.Endpoints.Secrets
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/secrets")]
    [Produces(MediaTypeNames.Application.Json)]
    public class List : Controller
    {
        private readonly ISecretsManager _secretsManager;
        public List(ISecretsManager secretsManager)
        {
            _secretsManager = secretsManager;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<Secret>))]
        public async Task<ActionResult<IEnumerable<Secret>>> Handle(CancellationToken cancellationToken = default)
        {
            var items = await _secretsManager.GetSecretViewModels(cancellationToken);

            return Json(items);
        }
    }
}
