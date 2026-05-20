using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Elsa.Identity.Constants;
using Elsa.Identity.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;

namespace Elsa.Identity.UnitTests.Options;

public class IdentityTokenOptionsTokenUseTests
{
    [Fact]
    public async Task AccessTokenSchemeRejectsRefreshToken()
    {
        var result = await ValidateTokenUseAsync(requiredTokenUse: TokenUse.Access, actualTokenUse: TokenUse.Refresh);

        Assert.NotNull(result.Failure);
    }

    [Fact]
    public async Task RefreshTokenSchemeRejectsAccessToken()
    {
        var result = await ValidateTokenUseAsync(requiredTokenUse: TokenUse.Refresh, actualTokenUse: TokenUse.Access);

        Assert.NotNull(result.Failure);
    }

    [Fact]
    public async Task AccessTokenSchemeAcceptsAccessToken()
    {
        var result = await ValidateTokenUseAsync(requiredTokenUse: TokenUse.Access, actualTokenUse: TokenUse.Access);

        Assert.Null(result.Failure);
    }

    [Fact]
    public async Task RefreshTokenSchemeAcceptsRefreshToken()
    {
        var result = await ValidateTokenUseAsync(requiredTokenUse: TokenUse.Refresh, actualTokenUse: TokenUse.Refresh);

        Assert.Null(result.Failure);
    }

    [Fact]
    public async Task AccessTokenSchemeRejectsTokenWithMissingTokenUseClaim()
    {
        var result = await ValidateTokenUseAsync(requiredTokenUse: TokenUse.Access, actualTokenUse: null);

        Assert.NotNull(result.Failure);
    }

    [Fact]
    public async Task OnTokenValidatedRunsPreviousHandlerBeforeTokenUseEnforcement()
    {
        var previousHandlerCalled = false;
        var identityOptions = new IdentityTokenOptions
        {
            SigningKey = IdentityTokenTestConstants.SigningKey
        };
        var jwtBearerOptions = new JwtBearerOptions
        {
            Events = new JwtBearerEvents
            {
                OnTokenValidated = context =>
                {
                    previousHandlerCalled = true;
                    context.Success();
                    return Task.CompletedTask;
                }
            }
        };
        identityOptions.ConfigureJwtBearerOptions(jwtBearerOptions, TokenUse.Access);

        var result = await ValidateTokenUseAsync(jwtBearerOptions, actualTokenUse: TokenUse.Refresh);

        Assert.True(previousHandlerCalled);
        Assert.NotNull(result.Failure);
    }

    [Fact]
    public async Task OnTokenValidatedPreservesPreviousNoResult()
    {
        var identityOptions = new IdentityTokenOptions
        {
            SigningKey = IdentityTokenTestConstants.SigningKey
        };
        var jwtBearerOptions = new JwtBearerOptions
        {
            Events = new JwtBearerEvents
            {
                OnTokenValidated = context =>
                {
                    context.NoResult();
                    return Task.CompletedTask;
                }
            }
        };
        identityOptions.ConfigureJwtBearerOptions(jwtBearerOptions, TokenUse.Access);

        var result = await ValidateTokenUseAsync(jwtBearerOptions, actualTokenUse: TokenUse.Access);

        Assert.True(result.None);
    }

    private static async Task<AuthenticateResult> ValidateTokenUseAsync(string requiredTokenUse, string? actualTokenUse)
    {
        var identityOptions = new IdentityTokenOptions
        {
            SigningKey = IdentityTokenTestConstants.SigningKey
        };
        var jwtBearerOptions = new JwtBearerOptions();
        identityOptions.ConfigureJwtBearerOptions(jwtBearerOptions, requiredTokenUse);
        return await ValidateTokenUseAsync(jwtBearerOptions, actualTokenUse);
    }

    public static async Task<AuthenticateResult> ValidateTokenUseAsync(JwtBearerOptions jwtBearerOptions, string? actualTokenUse)
    {
        var identityOptions = new IdentityTokenOptions
        {
            SigningKey = IdentityTokenTestConstants.SigningKey
        };
        var principal = ValidateToken(CreateToken(identityOptions, actualTokenUse), jwtBearerOptions.TokenValidationParameters, out var securityToken);
        var context = new TokenValidatedContext(
            new DefaultHttpContext(),
            new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, null, typeof(JwtBearerHandler)),
            jwtBearerOptions)
        {
            Principal = principal,
            SecurityToken = securityToken
        };

        await jwtBearerOptions.Events.TokenValidated(context);

        return context.Result ?? AuthenticateResult.Success(new AuthenticationTicket(principal, JwtBearerDefaults.AuthenticationScheme));
    }

    private static string CreateToken(IdentityTokenOptions options, string? tokenUse)
    {
        var now = DateTime.UtcNow;
        var credentials = new SigningCredentials(options.CreateSecurityKey(), SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Name, "alice")
        };

        if (tokenUse != null)
            claims.Add(new Claim(TokenUse.ClaimType, tokenUse));

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(5),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static ClaimsPrincipal ValidateToken(string token, TokenValidationParameters tokenValidationParameters, out SecurityToken securityToken)
    {
        return new JwtSecurityTokenHandler().ValidateToken(token, tokenValidationParameters, out securityToken);
    }
}
