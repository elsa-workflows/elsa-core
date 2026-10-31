using Elsa.Studio.Authentication.Abstractions.ComponentProviders;
using Elsa.Studio.Authentication.Abstractions.Contracts;
using Elsa.Studio.Authentication.Abstractions.Models;
using Elsa.Studio.Abstractions;
using Elsa.Studio.Contracts;
using Elsa.Studio.ExternalAuthentication.BlazorServer.Components;
using Elsa.Studio.ExternalAuthentication.BlazorServer.Services;
using Elsa.Studio.ExternalAuthentication.Models;
using Elsa.Studio.ExternalAuthentication.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.ExternalAuthentication.BlazorServer.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>Registers Studio Server as a confidential Elsa broker client.</summary>
    public static IServiceCollection AddExternalAuthenticationBroker(this IServiceCollection services, Action<ExternalAuthenticationClientOptions> configure)
    {
        var options = new ExternalAuthenticationClientOptions();
        configure(options);
        if (string.IsNullOrWhiteSpace(options.ClientId))
            throw new InvalidOperationException("External Authentication requires a configured broker client ID.");
        if (string.IsNullOrWhiteSpace(options.ClientSecret))
            throw new InvalidOperationException("Studio Server must configure a confidential broker client secret.");
        ValidateCallbackPaths(options.CallbackPath, options.LogoutCallbackPath);

        // The registration record is an instance descriptor; AddSingleton avoids TryAddEnumerable's
        // implementation-type ambiguity while preserving validation's enumerable contract.
        services.AddSingleton(new StudioAuthenticationProviderRegistration(StudioAuthenticationProvider.ExternalAuthentication));
        services.AddScoped<IFeature, Feature>();
        services.AddHttpContextAccessor();
        services.AddAntiforgery();
        services.AddMemoryCache();
        services.AddSingleton(options);
        services.AddSingleton<IServerExternalAuthenticationTransactionStore, ServerExternalAuthenticationTransactionStore>();
        services.AddSingleton<ServerExternalAuthenticationTicketStore>();
        services.AddSingleton<ServerExternalAuthenticationRefreshCoordinator>();
        services.AddScoped<ServerExternalAuthenticationStateProvider>();
        services.AddScoped<AuthenticationStateProvider>(provider => provider.GetRequiredService<ServerExternalAuthenticationStateProvider>());
        services.AddScoped<IExternalAuthenticationTokenProvider>(provider => provider.GetRequiredService<ServerExternalAuthenticationStateProvider>());
        services.AddScoped<IExternalAuthenticationAntiforgeryTokenProvider, ServerExternalAuthenticationAntiforgeryTokenProvider>();
        services.AddScoped<IHttpConnectionOptionsConfigurator, ExternalAuthenticationServerHttpConnectionOptionsConfigurator>();
        services.AddScoped<IExternalAuthenticationLoginCoordinator, ServerExternalAuthenticationLoginCoordinator>();
        services.AddExternalAuthenticationLoginUI();
        services.AddScoped<IUnauthorizedComponentProvider, UnauthorizedComponentProvider<ChallengeToExternalLogin>>();
        services.AddAuthorizationCore();
        services.AddAuthentication(authentication =>
            {
                authentication.DefaultAuthenticateScheme = ServerExternalAuthenticationStateProvider.Scheme;
                authentication.DefaultChallengeScheme = ServerExternalAuthenticationStateProvider.Scheme;
            })
            .AddCookie(ServerExternalAuthenticationStateProvider.Scheme, cookie =>
            {
                cookie.Cookie.Name = "ElsaStudio.ExternalAuthentication";
                cookie.Cookie.HttpOnly = true;
                cookie.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
                cookie.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
                cookie.LoginPath = "/login";
                cookie.ExpireTimeSpan = TimeSpan.FromHours(8);
                cookie.SlidingExpiration = false;
            });
        services.AddOptions<CookieAuthenticationOptions>(ServerExternalAuthenticationStateProvider.Scheme)
            .Configure<ServerExternalAuthenticationTicketStore>((cookie, ticketStore) => cookie.SessionStore = ticketStore);
        return services;
    }

    private static void ValidateCallbackPaths(string callbackPath, string logoutCallbackPath)
    {
        if (!string.Equals(callbackPath, ExternalAuthenticationCallbackPaths.SignIn, StringComparison.Ordinal) ||
            !string.Equals(logoutCallbackPath, ExternalAuthenticationCallbackPaths.Logout, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"External Authentication uses the fixed Studio callback paths '{ExternalAuthenticationCallbackPaths.SignIn}' and '{ExternalAuthenticationCallbackPaths.Logout}'.");
        }
    }
}
