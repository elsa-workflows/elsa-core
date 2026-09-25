using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Elsa.Studio.Localization.BlazorServer.Controllers;

/// <summary>
/// Controller for setting the culture.
/// </summary>
[Route("[controller]/[action]")]
/// <summary>
/// Represents the culture controller.
/// </summary>
public class CultureController : Controller
{
    /// <summary>
    /// Sets the culture and redirects to the specified URI.
    /// </summary>
    public IActionResult Set(string? culture, string redirectUri)
    {
        if (culture != null)
        {
            HttpContext.Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(
                    new RequestCulture(culture, culture)));
        }

        return LocalRedirect(redirectUri);
    }
}