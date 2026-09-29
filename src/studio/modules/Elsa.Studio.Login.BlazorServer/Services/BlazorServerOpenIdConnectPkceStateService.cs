using Elsa.Studio.Login.Contracts;
using Elsa.Studio.Login.Services;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace Elsa.Studio.Login.BlazorServer.Services;

/// inherit doc
public class BlazorServerOpenIdConnectPkceStateService : IOpenIdConnectPkceStateService
{
    private readonly ProtectedSessionStorage _protectedSessionStorage;

    /// inherit doc
    public BlazorServerOpenIdConnectPkceStateService(ProtectedSessionStorage protectedSessionStorage)
    {
        _protectedSessionStorage = protectedSessionStorage;
    }

    /// inherit doc
    public async Task<(string CodeChallenge, string Method)> GeneratePkceCodeChallenge()
    {
        var pkce = OpenIdConnectPkceCodeGenerator.GenerateCodeChallengeAndVerifier();
        await _protectedSessionStorage.SetAsync("pkceCodeVerifier", pkce.CodeVerifier);

        return (pkce.CodeChallenge, pkce.Method);
    }

    /// inherit doc
    public async Task<string> GetPkceCodeVerifier()
    {
        var verifier = await _protectedSessionStorage.GetAsync<string>("pkceCodeVerifier");
        return verifier.Success && verifier.Value != null ? verifier.Value : throw new Exception("PKCE code verifier not found in session storage.");
    }
}