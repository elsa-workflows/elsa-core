using Elsa.Samples.HttpEndpointSecurity.Services;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Samples.HttpEndpointSecurity.Endpoints.Tokens
{
    [ApiController]
    [Route("api/tokens")]
    public class Create : Controller
    {
        private readonly ITokenService _tokenService;
        public Create(ITokenService tokenService) => _tokenService = tokenService;

        [HttpPost]
        public IActionResult Handle(CreateTokenRequestModel model)
        {
            var token = _tokenService.CreateToken(model.UserName, model.IsAdmin);
            return Ok(token);
        }
    }
}