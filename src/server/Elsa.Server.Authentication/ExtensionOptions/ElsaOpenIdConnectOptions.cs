using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace Elsa.Server.Authentication.ExtensionOptions
{
    public class ElsaOpenIdConnectOptions : AuthenticationOptions
    {  

        public string DefaultChallengeScheme { get; set; } = OpenIdConnectDefaults.AuthenticationScheme;
        public override string LoginPath { get; set;} = "/signin-oidc";
        public string Authority { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string ResponseType { get; set; } = "code";
        public ICollection<string> Scopes { get; set; } = new List<string>();
        public Dictionary<string, string> UniqueJsonKeys = new Dictionary<string, string>();
        public bool UsePkce { get; set; } = false;
        public bool SaveTokens { get; set; } = true;
        public bool GetClaimsFromUserInfoEndpoint { get; set; } = false;
    }
}


 
 
