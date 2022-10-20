using Microsoft.AspNetCore.Authentication.Cookies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Server.Authentication.ExtensionOptions
{
    public class AuthenticationOptions
    {
        public string DefaultScheme { get; set; } = CookieAuthenticationDefaults.AuthenticationScheme;
        virtual public string LoginPath { get; set; } = "/account/login";
    }
}
