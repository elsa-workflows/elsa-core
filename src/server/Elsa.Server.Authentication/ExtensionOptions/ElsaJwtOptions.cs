using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Elsa.Server.Authentication.ExtensionOptions
{
    public class ElsaJwtOptions
    {
        public string DefaultJwtScheme { get; set; } = JwtBearerDefaults.AuthenticationScheme;
        public string IssuerSigningKey { get; set; }
        public string ValidIssuer { get; set; }
        public string Audience { get; set; }
        public bool RequireHttpsMetadata { get; set; } = false;
        public bool SaveToken { get; set; } = true;
        public bool ValidateIssuerSigningKey { get; set; } = true;
        public bool ValidateIssuer { get; set; } = true;
        public bool ValidateAudience { get; set; } = false;
   
    }
}
