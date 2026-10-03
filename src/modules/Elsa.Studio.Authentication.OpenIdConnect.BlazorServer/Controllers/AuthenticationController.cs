using Elsa.Studio.Authentication.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Controllers;

/// <summary>
/// Authentication entry points for initiating an OpenID Connect challenge/sign-out.
/// </summary>
[Route("authentication")]
public class AuthenticationController : Controller
{
    /// <summary>
    /// Triggers an OpenID Connect challenge.
    /// </summary>
    [HttpGet("login")]
    public IActionResult Login([FromQuery] string? returnUrl = null)
    {
        return Challenge(new AuthenticationProperties { RedirectUri = LocalReturnPath.Normalize(returnUrl) }, OpenIdConnectDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Signs out from both the local cookie and the OpenID Connect provider.
    /// </summary>
    /// <remarks>
    /// POST-only and antiforgery-protected so another site cannot sign the user out.
    /// </remarks>
    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public IActionResult Logout([FromForm] string? returnUrl = null)
    {
        return SignOut(
            new AuthenticationProperties { RedirectUri = LocalReturnPath.Normalize(returnUrl) },
            CookieAuthenticationDefaults.AuthenticationScheme,
            OpenIdConnectDefaults.AuthenticationScheme);
    }
}
