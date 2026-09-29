using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Elsa.Identity.Constants;
using Elsa.Identity.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using JsonWebToken = Microsoft.IdentityModel.JsonWebTokens.JsonWebToken;

namespace Elsa.Identity.Options;

/// <summary>
/// Represents options about token validation and generation.
/// </summary>
public class IdentityTokenOptions
{
    /// <summary>
    /// The key to use when signing tokens
    /// </summary>
    public string SigningKey { get; set; } = null!;
    
    /// <summary>
    /// The issuer to use when creating and validating tokens
    /// </summary>
    public string Issuer { get; set; } = "http://elsa.api";
    
    /// <summary>
    /// The audience to use when creating and validating tokens
    /// </summary>
    public string Audience { get; set; } = "http://elsa.api";
    
    /// <summary>
    /// The lifetime of access tokens
    /// </summary>
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(15);
    
    /// <summary>
    /// The lifetime of refresh tokens
    /// </summary>
    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromHours(2);
    
    /// <summary>
    /// Gets or sets the claim type that hold the tenant ID in the user's claims.
    /// If not set, <see cref="CustomClaimTypes.TenantId" /> will be used
    /// </summary>
    public string TenantIdClaimsType { get; set; } = CustomClaimTypes.TenantId;
    
    /// <summary>
    /// Creates a new <see cref="SecurityKey"/> from the <see cref="SigningKey"/>.
    /// </summary>
    /// <returns></returns>
    public SecurityKey CreateSecurityKey() => new SymmetricSecurityKey(Encoding.ASCII.GetBytes(SigningKey));

    /// <summary>
    /// Configures the <see cref="JwtBearerOptions"/> with the values from this instance.
    /// </summary>
    /// <param name="options">The options to configure.</param>
    public void ConfigureJwtBearerOptions(JwtBearerOptions options) => ConfigureJwtBearerOptions(options, TokenUse.Access);

    /// <summary>
    /// Configures the <see cref="JwtBearerOptions"/> with the values from this instance.
    /// </summary>
    /// <param name="options">The options to configure.</param>
    /// <param name="requiredTokenUse">The required token usage claim value.</param>
    /// <remarks>
    /// For <see cref="TokenUse.Refresh"/>, a token whose session was revoked is rejected too, using the request's
    /// <see cref="SessionRevoker"/>.
    /// </remarks>
    public void ConfigureJwtBearerOptions(JwtBearerOptions options, string requiredTokenUse)
    {
        options.TokenValidationParameters = CreateTokenValidationParameters();
        options.Events ??= new JwtBearerEvents();
        var previousOnTokenValidated = options.Events.OnTokenValidated;
        options.Events.OnTokenValidated = async context =>
        {
            await previousOnTokenValidated(context);

            if (context.Result?.Failure != null || context.Result?.None == true)
                return;

            var tokenUse = context.Principal?.FindFirst(TokenUse.ClaimType)?.Value;

            if (!string.Equals(tokenUse, requiredTokenUse, StringComparison.Ordinal))
            {
                context.Fail($"The token is not a valid {requiredTokenUse} token.");
                return;
            }

            if (requiredTokenUse == TokenUse.Refresh)
                await RejectRevokedSessionAsync(context);
        };
    }

    /// <summary>
    /// Creates token validation parameters for Elsa identity tokens.
    /// </summary>
    public TokenValidationParameters CreateTokenValidationParameters() => new()
    {
        IssuerSigningKey = CreateSecurityKey(),
        ValidAudience = Audience,
        ValidIssuer = Issuer,
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        LifetimeValidator = ValidateLifetime,
        NameClaimType = JwtRegisteredClaimNames.Name
    };

    private static async Task RejectRevokedSessionAsync(TokenValidatedContext context)
    {
        var identity = (ClaimsIdentity)context.Principal!.Identity!;
        var refreshToken = context.SecurityToken switch
        {
            JsonWebToken token => token.EncodedToken,
            JwtSecurityToken token => token.RawData,
            var token => throw new InvalidOperationException($"Cannot read a refresh token of type {token.GetType().Name}.")
        };
        var sessionId = SessionRevoker.GetSessionId(identity, refreshToken);
        var sessionRevoker = context.HttpContext.RequestServices.GetRequiredService<SessionRevoker>();

        if (await sessionRevoker.IsRevokedAsync(sessionId, context.HttpContext.RequestAborted))
        {
            context.Fail("The refresh token has been revoked.");
            return;
        }

        // Refreshing continues the session, including the one derived for a token issued before sessions existed.
        if (!identity.HasClaim(x => x.Type == CustomClaimTypes.SessionId))
            identity.AddClaim(new(CustomClaimTypes.SessionId, sessionId));
    }

    private static bool ValidateLifetime(DateTime? notBefore, DateTime? expires, SecurityToken securityToken, TokenValidationParameters validationParameters)
    {
        return expires != null && expires > DateTime.UtcNow;
    }
}
