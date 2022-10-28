using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Server.Authentication.ExtensionOptions
{
    public class ElsaCookieOptions : AuthenticationOptions
    {
        public SameSiteMode SameSite { get; set; } = SameSiteMode.Lax;
    }
}
