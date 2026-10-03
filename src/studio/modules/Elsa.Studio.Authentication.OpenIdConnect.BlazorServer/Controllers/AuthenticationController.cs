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
        return Challenge(new AuthenticationProperties { RedirectUri = SafeRedirectUri(returnUrl) }, OpenIdConnectDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Signs out from both the local cookie and the OpenID Connect provider.
    /// </summary>
    [HttpGet("logout")]
    public IActionResult Logout([FromQuery] string? returnUrl = null)
    {
        return SignOut(
            new AuthenticationProperties { RedirectUri = SafeRedirectUri(returnUrl) },
            CookieAuthenticationDefaults.AuthenticationScheme,
            OpenIdConnectDefaults.AuthenticationScheme);
    }

    private string SafeRedirectUri(string? returnUrl)
    {
        var path = LocalReturnPath.Normalize(returnUrl);
        if (!Url.IsLocalUrl(path))
        {
            return "/";
        }

        return path;
    }
}
