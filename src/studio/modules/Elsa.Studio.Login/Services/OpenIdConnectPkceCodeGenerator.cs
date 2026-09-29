using Microsoft.AspNetCore.WebUtilities;
using System.Security.Cryptography;
using System.Text;

namespace Elsa.Studio.Login.Services;

/// <summary>
/// Used to generate PKCE (Proof Key for Code Exchange) codes for use in the authorization code flow.
/// </summary>
public static class OpenIdConnectPkceCodeGenerator
{
    /// <summary>
    /// Generates PKCE (Proof Key for Code Exchange) code challenge and verifier.
    /// </summary>
    public static (string CodeChallenge, string CodeVerifier, string Method) GenerateCodeChallengeAndVerifier()
    {
        using var rng = RandomNumberGenerator.Create();
        var randomBytes = new byte[32];
        rng.GetBytes(randomBytes);
        var verifier = WebEncoders.Base64UrlEncode(randomBytes);
        var buffer = Encoding.UTF8.GetBytes(verifier);
        var hash = SHA256.HashData(buffer);
        var challenge = WebEncoders.Base64UrlEncode(hash);

        return (CodeChallenge: challenge, CodeVerifier: verifier, Method: "S256");
    }
}