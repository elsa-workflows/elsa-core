using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Elsa.Identity.Options;

public class IdentityTokenOptions
{
    public string SigningKey { get; set; }
    public string Issuer { get; set; } = "http://elsa.api";
    public string Audience { get; set; } = "http://elsa.api";
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromHours(1);
    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromHours(2);
    
    public SecurityKey CreateSecurityKey() => new SymmetricSecurityKey(Encoding.ASCII.GetBytes(SigningKey));

    public void ConfigureJwtBearerOptions(JwtBearerOptions options) =>
        options.TokenValidationParameters = new TokenValidationParameters
        {
            IssuerSigningKey = CreateSecurityKey(),
            ValidAudience = Audience,
            ValidIssuer = Issuer,
            ValidateLifetime = true,
            LifetimeValidator = ValidateLifetime,
            NameClaimType = JwtRegisteredClaimNames.Name
        };

    private bool ValidateLifetime(DateTime? notBefore, DateTime? expires, SecurityToken securityToken, TokenValidationParameters validationParameters)
    {
        return expires != null && expires > DateTime.UtcNow;
    }

    /// <summary>
    /// Deconstructor.
    /// </summary>
    internal void Deconstruct(out string signingKey, out string issuer, out string audience, out TimeSpan accessTokenLifetime, out TimeSpan refreshTokenLifetime)
    {
        signingKey = SigningKey;
        issuer = Issuer;
        audience = Audience;
        accessTokenLifetime = AccessTokenLifetime;
        refreshTokenLifetime = RefreshTokenLifetime;
    }

    internal void CopyFrom(IdentityTokenOptions identityOptions)
    {
        SigningKey = identityOptions.SigningKey;
        Audience = identityOptions.Audience;
        Issuer = identityOptions.Issuer;
        AccessTokenLifetime = identityOptions.AccessTokenLifetime;
        RefreshTokenLifetime = identityOptions.RefreshTokenLifetime;
    }
}