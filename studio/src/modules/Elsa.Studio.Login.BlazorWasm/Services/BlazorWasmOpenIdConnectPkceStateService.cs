using Elsa.Studio.Login.Contracts;
using Elsa.Studio.Login.Services;
using Microsoft.JSInterop;

namespace Elsa.Studio.Login.BlazorWasm.Services;

/// inherit doc
public class BlazorWasmOpenIdConnectPkceStateService : IOpenIdConnectPkceStateService
{
    private readonly IJSRuntime _js;

    /// inherit doc  
    public BlazorWasmOpenIdConnectPkceStateService(IJSRuntime js)
    {
        _js = js;
    }

    /// inherit doc
    public async Task<(string CodeChallenge, string Method)> GeneratePkceCodeChallenge()
    {
        var pkce = OpenIdConnectPkceCodeGenerator.GenerateCodeChallengeAndVerifier();
        await _js.InvokeVoidAsync("sessionStorage.setItem", "pkceCodeVerifier", pkce.CodeVerifier);

        return (pkce.CodeChallenge, pkce.Method);
    }

    /// inherit doc
    public async Task<string> GetPkceCodeVerifier()
    {
        return await _js.InvokeAsync<string>("sessionStorage.getItem", "pkceCodeVerifier");
    }
}