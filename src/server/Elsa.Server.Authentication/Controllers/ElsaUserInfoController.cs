using Elsa.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Elsa.Server.Authentication.Controllers
{
    [ApiController]
    [ApiVersion("1")]
    [Route("v{apiVersion:apiVersion}/ElsaAuthentication/UserInfo")]
    [Produces("application/json")]
    public class ElsaUserInfoController : Controller
    {
       private readonly ITenantAccessor _tenantAccessor;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ElsaUserInfoController(ITenantAccessor tenantAccessor, IHttpContextAccessor httpContextAccessor)
        {
            this._tenantAccessor = tenantAccessor;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Handle()
        {
           var name = _httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name || c.Type == "name")?.Value ?? null;
           var tenantId  = await _tenantAccessor.GetTenantIdAsync() ?? null;
            return Ok(new { IsAuthenticated =_httpContextAccessor.HttpContext.User?.Identity?.IsAuthenticated ?? false,name = name, tenantId = tenantId });
        }
    }
}
