using Elsa.Server.Authentication.Contexts;
using Elsa.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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
 
    [Route("v{apiVersion:apiVersion}/ElsaAuthentication/logout")]
    [Produces("application/json")]
    public class ElsaUserLogoutController : Controller
    {
       private readonly ITenantAccessor _tenantAccessor;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ElsaUserLogoutController(ITenantAccessor tenantAccessor, IHttpContextAccessor httpContextAccessor)
        {
            this._tenantAccessor = tenantAccessor;
            _httpContextAccessor = httpContextAccessor;
        }


        [HttpGet]
        public async Task Handle()
        {
            if (ElsaAuthenticationContext.AuthenticationStyles.Contains(AuthenticationStyles.OpenIdConnect))
            {
                await _httpContextAccessor.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                await _httpContextAccessor.HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
            }
            if (ElsaAuthenticationContext.AuthenticationStyles.Contains(AuthenticationStyles.ServerManagedCookie))
            {
                await _httpContextAccessor.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                _httpContextAccessor.HttpContext.Response.Redirect("/");
            }
        }
    }
}
