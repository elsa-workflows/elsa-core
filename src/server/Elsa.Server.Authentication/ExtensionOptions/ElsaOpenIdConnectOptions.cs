using Microsoft.AspNetCore.Authentication.Cookies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Server.Authentication.ExtensionOptions
{
    public class ElsaOpenIdConnectOptions : AuthenticationOptions
    {  
        public string DefaultChallengeScheme { get; set; } = CookieAuthenticationDefaults.AuthenticationScheme;
        public override string LoginPath { get; set;} = "/signin-oidc";
        public string Authority { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string ResponseType { get; set; }   
        public ICollection<string> Scopes { get; set; } = new List<string>();
        public bool UsePkce { get; set; } = false;
        public bool SaveTokens { get; set; } = true;
       
        public bool GetClaimsFromUserInfoEndpoint { get; set; } = false;
    }
}


 
 
