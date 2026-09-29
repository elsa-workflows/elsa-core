using Elsa.Studio.Authentication.Abstractions.ComponentProviders;
using Elsa.Studio.Authentication.Abstractions.Contracts;
using Elsa.Studio.Authentication.Abstractions.Models;
using Elsa.Studio.Authentication.OpenIdConnect.Contracts;
using Elsa.Studio.Authentication.OpenIdConnect.Models;
using Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Services;
using Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Components;
using Elsa.Studio.Contracts;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

namespace Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Extensions;

/// <summary>
/// Extension methods for configuring OpenID Connect authentication in Blazor Server.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Default retry policy: 3 retries with exponential backoff (1s, 2s, 4s) for transient HTTP failures.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> DefaultRetryPolicy => HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(r => r.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt - 1)));

    /// <summary>
    /// Adds OpenID Connect authentication services for Blazor Server.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration callback for OIDC options.</param>
    /// <param name="configureRetryPolicy">Optional factory to provide a custom retry policy. If null, uses <see cref="DefaultRetryPolicy"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddOpenIdConnectAuth(
        this IServiceCollection services,
        Action<OidcOptions> configure,
        Func<IAsyncPolicy<HttpResponseMessage>>? configureRetryPolicy = null)
    {
        var options = new OidcOptions();
        configure(options);
        services.AddStudioAuthenticationProviderRegistration(StudioAuthenticationProvider.OpenIdConnect);

        // Ensure we always request the minimal identity scopes.
        var configuredScopes = options.AuthenticationScopes?.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray() ?? Array.Empty<string>();
        if (configuredScopes.Length == 0)
            configuredScopes = ["openid", "profile", "offline_access"];
        options.AuthenticationScopes = configuredScopes;

        // Set Blazor Server defaults for callback paths if not explicitly specified.
        options.CallbackPath ??= "/signin-oidc";
        options.SignedOutCallbackPath ??= "/signout-callback-oidc";

        // Register core services
        services.AddHttpContextAccessor();
        services.AddSingleton(options);
        services.AddScoped<ITokenProvider, ServerTokenProvider>();
        services.AddScoped<IHttpConnectionOptionsConfigurator, OpenIdConnect.Services.OidcHttpConnectionOptionsConfigurator>();
        
        // Shared token refresh service used by both session refresh and backend API token acquisition
        services.AddScoped<TokenRefreshService>();
        
        // Cookie authentication events for automatic session token refresh (standard ASP.NET Core pattern)
        services.AddScoped<AuthCookieEvents>();

        // Configure ASP.NET Core authentication with cookie and OIDC
        services.AddAuthentication(authOptions =>
            {
                authOptions.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                authOptions.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, cookieOptions =>
            {
                cookieOptions.Cookie.Name = "ElsaStudio.Auth";
                cookieOptions.Cookie.HttpOnly = true;
                cookieOptions.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
                cookieOptions.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
                cookieOptions.ExpireTimeSpan = TimeSpan.FromHours(8);
                cookieOptions.SlidingExpiration = true;
                
                // Use custom events for automatic token refresh on every request validation
                cookieOptions.EventsType = typeof(AuthCookieEvents);
            })
            .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, oidcOptions =>
            {
                oidcOptions.Authority = options.Authority;
                oidcOptions.ClientId = options.ClientId;
                oidcOptions.ClientSecret = options.ClientSecret;
                oidcOptions.ResponseType = options.ResponseType;
                oidcOptions.UsePkce = options.UsePkce;
                oidcOptions.SaveTokens = options.SaveTokens;
                oidcOptions.CallbackPath = options.CallbackPath;
                oidcOptions.SignedOutCallbackPath = options.SignedOutCallbackPath;
                oidcOptions.RequireHttpsMetadata = options.RequireHttpsMetadata;
                oidcOptions.GetClaimsFromUserInfoEndpoint = options.GetClaimsFromUserInfoEndpoint;

                // Configure scopes
                oidcOptions.Scope.Clear();
                foreach (var scope in options.AuthenticationScopes)
                {
                    oidcOptions.Scope.Add(scope);
                }

                // Map token response properties to enable token refresh
                oidcOptions.MapInboundClaims = false;
                
                if (!string.IsNullOrWhiteSpace(options.MetadataAddress))
                {
                    oidcOptions.MetadataAddress = options.MetadataAddress;
                }

                // Configure token validation parameters
                oidcOptions.TokenValidationParameters = new()
                {
                    NameClaimType = options.NameClaimType,
                    RoleClaimType = options.RoleClaimType,
                    ValidateIssuer = true
                };
            });

        // Add authorization services
        services.AddAuthorizationCore();

        // Use an OIDC-aware unauthorized component that initiates a challenge.
        services.AddScoped<IUnauthorizedComponentProvider, UnauthorizedComponentProvider<ChallengeToLogin>>();
        services.AddScoped<ILoginMethodCatalog, DirectOpenIdConnectLoginMethodCatalog>();
        services.AddScoped<ILoginMethodComponentProvider, DirectOpenIdConnectLoginMethodComponentProvider>();
        
        // HTTP client for token refresh requests with retry policy
        var retryPolicy = configureRetryPolicy?.Invoke() ?? DefaultRetryPolicy;
        services.AddHttpClient(TokenRefreshService.AnonymousHttpClientName)
            .AddPolicyHandler(retryPolicy);

        return services;
    }
}
