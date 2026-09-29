using Elsa.Studio.Authentication.OpenIdConnect.Contracts;
using Elsa.Studio.Authentication.OpenIdConnect.Models;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace Elsa.Studio.Authentication.OpenIdConnect.BlazorWasm.Services;

/// <summary>
/// Blazor WASM implementation of <see cref="IOidcTokenAccessor"/> that uses the built-in token provider.
/// </summary>
/// <remarks>
/// This accessor supports scope-aware token requests, enabling incremental consent scenarios
/// where different tokens are needed for different API audiences (e.g., Graph vs. backend API).
/// </remarks>
public class WasmTokenProvider(IAccessTokenProvider tokenProvider, OidcOptions oidcOptions) : ITokenProvider
{
    /// <inheritdoc />
    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        // For WASM, we use the IAccessTokenProvider to get the current access token
        // The framework handles token refresh automatically

        var scopes = oidcOptions.BackendApiScopes;
        var requestedScopes = scopes.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();

        var tokenResult = await tokenProvider.RequestAccessToken(new()
        {
            Scopes = requestedScopes
        });

        return tokenResult.TryGetToken(out var token) ? token.Value : null;
    }
}