using System.Threading;
using System.Threading.Tasks;
using Elsa.Secrets.Extensions;
using Elsa.Secrets.Http.Services;
using Elsa.Secrets.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Elsa.Secrets.Api.Endpoints.OAuth2
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/oauth2/callback")]
    [Produces("application/json")]
    public class SetAuthCodeCallback : Controller
    {
        private readonly ISecretsStore _secretsStore;
        private readonly IOAuth2TokenService _tokenService;
        private readonly IConfiguration _configuration;
        
        public SetAuthCodeCallback(ISecretsStore secretsStore, IOAuth2TokenService tokenService, IConfiguration configuration)
        {
            _secretsStore = secretsStore;
            _tokenService = tokenService;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<ActionResult> Handle(string state, string code, CancellationToken cancellationToken = default)
        {
            var secret = await _secretsStore.FindByIdAsync(state, cancellationToken);

            if (secret == null)
                return NotFound();

            if (secret?.Id != null)
            {
                var token = await _tokenService.GetToken(secret, code, $"{_configuration["Elsa:Server:BaseUrl"]}/v1/oauth2/callback");
                if (!string.IsNullOrEmpty(token.Error))
                    return BadRequest();
            }

            var baseUrl = _configuration["Elsa:Server:FrontendBaseUrl"] ?? _configuration["Elsa:Server:BaseUrl"];
            return Redirect($"{baseUrl}/oauth2-authorized");
        }
    }
}
