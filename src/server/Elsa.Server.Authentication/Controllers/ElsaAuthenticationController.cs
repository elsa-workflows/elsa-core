using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Server.Authentication.Controllers
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/ElsaAuthentication/UserInfo")]
    [Produces("application/json")]
    public class ElsaAuthenticationController : Controller
    {
        public ElsaAuthenticationController()
        {

        }
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Handle()
        {
            return Ok(new { name = "ibrahim nada11" });
        }
    }
}
