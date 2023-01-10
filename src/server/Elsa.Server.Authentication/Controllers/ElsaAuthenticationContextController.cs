using Elsa.Server.Authentication.Contexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Server.Authentication.Controllers
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/ElsaAuthentication/options")]
    [Produces("application/json")]
    public class ElsaAuthenticationContextController : Controller
    {
        [AllowAnonymous]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public Task<IActionResult> Handle()
        {
            return Task.FromResult((IActionResult) Ok(new
            {
                AuthenticationStyles = ElsaAuthenticationContext.AuthenticationStyles.ConvertAll(x => x.ToString()),
                ElsaAuthenticationContext.CurrentTenantAccessorName, ElsaAuthenticationContext.TenantAccessorKeyName
            }));
        }
    }
}