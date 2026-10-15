using System.Security.Claims;

namespace Elsa.Studio.Authentication.ElsaIdentity.Contracts;

/// <summary>
/// Parses JWT tokens into claims.
/// </summary>
public interface IJwtParser
{
    /// <summary>
    /// Parses the specified JWT and returns claims.
    /// </summary>
    IEnumerable<Claim> Parse(string token);
}

