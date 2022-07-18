using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Elsa.Api.Common;
using Elsa.Api.Common.Options;
using Elsa.Api.Common.Services;
using Elsa.WorkflowServer.Web.Models;
using FastEndpoints.Security;
using Microsoft.Extensions.Options;

namespace Elsa.WorkflowServer.Web.Implementations;

public class CustomAccessTokenIssuer : IAccessTokenIssuer
{
    private readonly AccessTokenOptions _options;

    public CustomAccessTokenIssuer(IOptions<AccessTokenOptions> options)
    {
        _options = options.Value;
    }

    public ValueTask<string> CreateTokenAsync(IDictionary<string, string> credentials, object? context, CancellationToken cancellationToken = default)
    {
        var user = context as User;
        var fullName = user!.FullName;

        var jwtToken = JWTBearer.CreateToken(
            signingKey: _options.SigningKey,
            roles: new[] { RoleNames.Admin },
            permissions: new[] { PermissionNames.Everything },
            claims: new[] { new Claim(JwtRegisteredClaimNames.Name, fullName) }
        );

        return new(jwtToken);
    }
}