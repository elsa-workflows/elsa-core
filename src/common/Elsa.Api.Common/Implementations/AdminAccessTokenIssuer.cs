using Elsa.Api.Common.Options;
using Elsa.Api.Common.Services;
using FastEndpoints.Security;
using Microsoft.Extensions.Options;

namespace Elsa.Api.Common.Implementations;

/// <summary>
/// Creates access tokens that grant full access to all Elsa API endpoints by means of the Admin role and Everything permissions.
/// </summary>
public class AdminAccessTokenIssuer : IAccessTokenIssuer
{
    private readonly AccessTokenOptions _options;

    public AdminAccessTokenIssuer(IOptions<AccessTokenOptions> options)
    {
        _options = options.Value;
    }

    public ValueTask<string> CreateTokenAsync(IDictionary<string, string> credentials, object? context, CancellationToken cancellationToken = default)
    {
        var jwtToken = JWTBearer.CreateToken(
            signingKey: _options.SigningKey,
            roles: new[] { RoleNames.Admin },
            permissions: new[] { PermissionNames.Everything });

        return new(jwtToken);
    }
}