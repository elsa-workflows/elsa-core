using System.Security.Claims;

namespace Elsa.Studio.Login.Contracts;

/// <summary>
/// Parses a JWT token and returns the claims.
/// </summary>
public interface IJwtParser
{
    /// <summary>
    /// Parses a JWT token and returns the claims.
    /// </summary>
    IEnumerable<Claim> Parse(string jwt);
}