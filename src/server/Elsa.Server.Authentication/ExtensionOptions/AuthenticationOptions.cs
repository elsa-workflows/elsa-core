using Microsoft.AspNetCore.Authentication.Cookies;

namespace Elsa.Server.Authentication.ExtensionOptions
{
    public class AuthenticationOptions
    {
        public string DefaultScheme { get; set; } = CookieAuthenticationDefaults.AuthenticationScheme;
        virtual public string LoginPath { get; set; } = "/account/login";
    }
}
