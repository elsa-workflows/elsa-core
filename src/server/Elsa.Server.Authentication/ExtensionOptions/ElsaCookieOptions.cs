using Microsoft.AspNetCore.Http;

namespace Elsa.Server.Authentication.ExtensionOptions
{
    public class ElsaCookieOptions : AuthenticationOptions
    {
        public SameSiteMode SameSite { get; set; } = SameSiteMode.Lax;
    }
}
