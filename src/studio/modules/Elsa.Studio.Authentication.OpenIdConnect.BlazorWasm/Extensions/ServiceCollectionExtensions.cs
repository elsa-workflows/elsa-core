using Elsa.Studio.Authentication.Abstractions.ComponentProviders;
using Elsa.Studio.Authentication.Abstractions.Contracts;
using Elsa.Studio.Authentication.Abstractions.Models;
using Elsa.Studio.Authentication.OpenIdConnect.Contracts;
using Elsa.Studio.Authentication.OpenIdConnect.Models;
using Elsa.Studio.Authentication.OpenIdConnect.BlazorWasm.Services;
using Elsa.Studio.Authentication.OpenIdConnect.BlazorWasm.Components;
using Elsa.Studio.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Authentication.OpenIdConnect.BlazorWasm.Extensions;

/// <summary>
/// Extension methods for configuring OpenID Connect authentication in Blazor WebAssembly.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds OpenID Connect authentication services for Blazor WebAssembly.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration callback for OIDC options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddOpenIdConnectAuth(
        this IServiceCollection services,
        Action<OidcOptions> configure)
    {
        var options = new OidcOptions();
        configure(options);
        services.AddStudioAuthenticationProviderRegistration(StudioAuthenticationProvider.OpenIdConnect);

        // Set Blazor WASM defaults for callback paths if not explicitly specified.
        options.CallbackPath ??= "/authentication/login-callback";
        options.SignedOutCallbackPath ??= "/authentication/logout-callback";

        // Register options for access by services
        services.AddSingleton(options);

        // Register the token accessor
        services.AddScoped<ITokenProvider, WasmTokenProvider>();
        services.AddScoped<IHttpConnectionOptionsConfigurator, OpenIdConnect.Services.OidcHttpConnectionOptionsConfigurator>();
        services.AddScoped<IFeature, OpenIdConnectBlazorWasmFeature>();

        // Configure WASM authentication using the built-in framework.
        // Note: Entra ID requires absolute redirect URIs.
        services.AddOidcAuthentication(wasmOptions =>
        {
            wasmOptions.ProviderOptions.Authority = options.Authority;
            wasmOptions.ProviderOptions.ClientId = options.ClientId;
            wasmOptions.ProviderOptions.ResponseType = options.ResponseType;
            
            var scopes = options.AuthenticationScopes
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Ensure we always request at least the OIDC basics.
            if (scopes.Count == 0)
                scopes.AddRange(["openid", "profile"]);

            // Clear any default scopes that might have been added by the framework
            wasmOptions.ProviderOptions.DefaultScopes.Clear();

            foreach (var scope in scopes)
                wasmOptions.ProviderOptions.DefaultScopes.Add(scope);

            if (!string.IsNullOrWhiteSpace(options.MetadataAddress))
                wasmOptions.ProviderOptions.MetadataUrl = options.MetadataAddress;

        });

        // Provide an OIDC-aware unauthorized component.
        services.AddScoped<IUnauthorizedComponentProvider, UnauthorizedComponentProvider<NavigateToLogin>>();
        services.AddScoped<ILoginMethodCatalog, DirectOpenIdConnectLoginMethodCatalog>();
        services.AddScoped<ILoginMethodComponentProvider, DirectOpenIdConnectLoginMethodComponentProvider>();

        return services;
    }
}
