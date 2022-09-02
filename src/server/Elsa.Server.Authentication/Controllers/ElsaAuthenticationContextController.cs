using Elsa.Server.Authentication.Contexts;
using Elsa.Services;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

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
        public async Task<IActionResult> Handle()
        {
            return Ok(new { AuthenticationStyles = ElsaAuthenticationContext.AuthenticationStyles.ConvertAll(x=>x.ToString()) , 
                ElsaAuthenticationContext.CurrentTenantAccessorName , ElsaAuthenticationContext.TenantAccessorKeyName });
        }
    }
}
