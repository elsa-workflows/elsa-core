using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Elsa.Studio.Login.Contracts;

namespace Elsa.Studio.Login.BlazorServer.Services;

/// <inheritdoc />
public class BlazorServerJwtParser : IJwtParser
{
    /// <inheritdoc />
    public IEnumerable<Claim> Parse(string jwt)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwtSecurityToken = handler.ReadJwtToken(jwt);
        return jwtSecurityToken.Claims;
    }
}