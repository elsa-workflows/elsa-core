using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Elsa.Identity.Options;

public class IdentityOptions
{
    public string SigningKey { get; set; }
    public string Issuer { get; set; } = "http://elsa.api";
    public string Audience { get; set; } = "http://elsa.api";
    public TimeSpan? Lifetime { get; set; } = TimeSpan.FromHours(1);
    
    public SecurityKey CreateSecurityKey() => new SymmetricSecurityKey(Encoding.ASCII.GetBytes(SigningKey));

    public void ConfigureJwtBearerOptions(JwtBearerOptions options) =>
        options.TokenValidationParameters = new TokenValidationParameters
        {
            IssuerSigningKey = CreateSecurityKey(),
            ValidAudience = Audience,
            ValidIssuer = Issuer
        };

    /// <summary>
    /// Deconstructor.
    /// </summary>
    internal void Deconstruct(out string signingKey, out string issuer, out string audience, out TimeSpan? lifetime)
    {
        signingKey = SigningKey;
        issuer = Issuer;
        audience = Audience;
        lifetime = Lifetime;
    }

    internal void CopyFrom(IdentityOptions identityOptions)
    {
        SigningKey = identityOptions.SigningKey;
        Audience = identityOptions.Audience;
        Issuer = identityOptions.Issuer;
        Lifetime = identityOptions.Lifetime;
    }
}