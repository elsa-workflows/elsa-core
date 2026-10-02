using Blazored.LocalStorage;
using Elsa.Studio.Authentication.Abstractions.ComponentProviders;
using Elsa.Studio.Authentication.Abstractions.Contracts;
using Elsa.Studio.Components;
using Elsa.Studio.Contracts;
using Elsa.Studio.Authentication.ElsaIdentity.Contracts;
using Elsa.Studio.Authentication.ElsaIdentity.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Authentication.ElsaIdentity.Extensions;

/// <summary>
/// Service registration extensions for the ElsaIdentity authentication module.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the ElsaIdentity core services (provider-agnostic); call one of the <c>Use*</c> methods to select an auth flow.
    /// </summary>
    public static IServiceCollection AddElsaIdentityCore(this IServiceCollection services)
    {
        services
            .AddOptions()
            .AddAuthorizationCore()
            .AddBlazoredLocalStorage()
            .AddScoped<AuthenticationStateProvider, AccessTokenAuthenticationStateProvider>()
            .AddScoped<IUnauthorizedComponentProvider, UnauthorizedComponentProvider<Unauthorized>>()
            .AddScoped<ICredentialsValidator, ElsaIdentityCredentialsValidator>()
            .AddScoped<ITokenProvider, JwtTokenProvider>()
            .AddSingleton<IJwtParser, JwtParser>()
            .AddScoped<IHttpConnectionOptionsConfigurator, ElsaIdentityHttpConnectionOptionsConfigurator>();

        services.AddHttpClient(ElsaIdentityRefreshTokenService.AnonymousClientName);
        services.AddScoped<IRefreshTokenService, ElsaIdentityRefreshTokenService>();

        return services;
    }
}